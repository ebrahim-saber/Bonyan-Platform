using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContractingPlatform.Application.DTOs.Bids;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Web.Controllers;

public class BidsController : Controller
{
    private readonly IBidService _bidService;
    private readonly IProjectService _projectService;
    private readonly ICurrentUserService _currentUserService;

    public BidsController(
        IBidService bidService,
        IProjectService projectService,
        ICurrentUserService currentUserService)
    {
        _bidService = bidService;
        _projectService = projectService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserType.Contractor))]
    public async Task<IActionResult> Submit(int projectId)
    {
        var projectRes = await _projectService.GetProjectDetailsAsync(projectId, _currentUserService.UserId, null, User.IsInRole(nameof(UserType.Admin)));
        if (!projectRes.Success || projectRes.Data == null)
        {
            TempData["ErrorMessage"] = "المشروع غير موجود";
            return RedirectToAction("Index", "Projects");
        }

        ViewBag.Project = projectRes.Data;
        return View(new CreateBidDto { ProjectRequestId = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = nameof(UserType.Contractor))]
    public async Task<IActionResult> Submit(CreateBidDto dto)
    {
        var contractorProfileId = await _currentUserService.GetContractorProfileIdAsync();
        if (!contractorProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "تعذر تحديد حساب المقاول";
            return RedirectToAction("Login", "Account");
        }

        var result = await _bidService.SubmitBidAsync(dto, contractorProfileId.Value);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Details", "Projects", new { id = dto.ProjectRequestId });
        }

        var projectRes = await _projectService.GetProjectDetailsAsync(dto.ProjectRequestId, _currentUserService.UserId, null, User.IsInRole(nameof(UserType.Admin)));
        ViewBag.Project = projectRes.Data;
        ModelState.AddModelError("", result.Message);
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = nameof(UserType.Client))]
    public async Task<IActionResult> Accept(int bidId)
    {
        var clientProfileId = await _currentUserService.GetClientProfileIdAsync();
        if (!clientProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "تعذر التحقق من حساب العميل";
            return RedirectToAction("Login", "Account");
        }

        var acceptDto = new AcceptBidDto { BidId = bidId };
        var result = await _bidService.AcceptBidAsync(acceptDto, clientProfileId.Value);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            if (result.Data > 0)
            {
                return RedirectToAction("Details", "Contracts", new { id = result.Data });
            }
            return RedirectToAction("MyContracts", "Contracts");
        }

        TempData["ErrorMessage"] = result.Message;
        return RedirectToAction("MyProjects", "Projects");
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserType.Contractor))]
    public async Task<IActionResult> MyBids()
    {
        var contractorProfileId = await _currentUserService.GetContractorProfileIdAsync();
        if (!contractorProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "تعذر التحقق من حساب المقاول";
            return RedirectToAction("Login", "Account");
        }

        var bids = await _bidService.GetContractorBidsAsync(contractorProfileId.Value);
        return View(bids);
    }
}
