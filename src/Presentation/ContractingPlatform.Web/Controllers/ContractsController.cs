using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContractingPlatform.Application.DTOs.Contracts;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Web.Controllers;

[Authorize]
public class ContractsController : Controller
{
    private readonly IContractService _contractService;
    private readonly ICurrentUserService _currentUserService;

    public ContractsController(IContractService contractService, ICurrentUserService currentUserService)
    {
        _contractService = contractService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _contractService.GetContractDetailsAsync(id, _currentUserService.UserId, User.IsInRole(nameof(UserType.Admin)));
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(MyContracts));
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> MyContracts()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        UserType userType = User.IsInRole(nameof(UserType.Contractor)) ? UserType.Contractor : UserType.Client;
        var contracts = await _contractService.GetUserContractsAsync(userId, userType);
        return View(contracts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = nameof(UserType.Contractor))]
    public async Task<IActionResult> SubmitMilestone(SubmitMilestoneProofDto dto, int contractId)
    {
        var contractorProfileId = await _currentUserService.GetContractorProfileIdAsync();
        if (!contractorProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "تعذر تحديد حساب المقاول";
            return RedirectToAction("Login", "Account");
        }

        var result = await _contractService.SubmitMilestoneProofAsync(dto, contractorProfileId.Value);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = contractId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = nameof(UserType.Client))]
    public async Task<IActionResult> ApproveMilestone(int milestoneId, int contractId, string? notes)
    {
        var clientProfileId = await _currentUserService.GetClientProfileIdAsync();
        if (!clientProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "تعذر تحديد حساب العميل";
            return RedirectToAction("Login", "Account");
        }

        var result = await _contractService.ApproveMilestoneAndReleasePaymentAsync(milestoneId, clientProfileId.Value, notes);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = contractId });
    }
}
