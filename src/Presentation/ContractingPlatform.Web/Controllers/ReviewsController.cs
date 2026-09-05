using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContractingPlatform.Application.DTOs.Reviews;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Web.Controllers;

[Authorize(Roles = nameof(UserType.Client))]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly IContractService _contractService;
    private readonly ICurrentUserService _currentUserService;

    public ReviewsController(
        IReviewService reviewService,
        IContractService contractService,
        ICurrentUserService currentUserService)
    {
        _reviewService = reviewService;
        _contractService = contractService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int contractId)
    {
        var contractRes = await _contractService.GetContractDetailsAsync(contractId);
        if (!contractRes.Success || contractRes.Data == null)
        {
            TempData["ErrorMessage"] = "العقد غير موجود";
            return RedirectToAction("MyContracts", "Contracts");
        }

        ViewBag.Contract = contractRes.Data;
        return View(new CreateReviewDto
        {
            ProjectContractId = contractId,
            OverallRating = 5,
            QualityRating = 5,
            PunctualityRating = 5,
            CommunicationRating = 5
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReviewDto dto)
    {
        var clientProfileId = await _currentUserService.GetClientProfileIdAsync();
        if (!clientProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "تعذر تحديد حساب العميل";
            return RedirectToAction("Login", "Account");
        }

        var result = await _reviewService.SubmitReviewAsync(dto, clientProfileId.Value);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Details", "Contracts", new { id = dto.ProjectContractId });
        }

        var contractRes = await _contractService.GetContractDetailsAsync(dto.ProjectContractId);
        ViewBag.Contract = contractRes.Data;
        ModelState.AddModelError("", result.Message);
        return View(dto);
    }
}
