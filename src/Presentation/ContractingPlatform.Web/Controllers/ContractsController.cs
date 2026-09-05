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
    private readonly IFileStorageService _fileStorageService;

    public ContractsController(
        IContractService contractService, 
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService)
    {
        _contractService = contractService;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
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
    public async Task<IActionResult> SubmitMilestone(SubmitMilestoneProofDto dto, int contractId, IFormFile? proofFile)
    {
        var contractorProfileId = await _currentUserService.GetContractorProfileIdAsync();
        if (!contractorProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "تعذر تحديد حساب المقاول";
            return RedirectToAction("Login", "Account");
        }

        if (proofFile != null && proofFile.Length > 0)
        {
            if (!_fileStorageService.IsAllowedExtension(proofFile.FileName))
            {
                TempData["ErrorMessage"] = $"نوع ملف الإثبات '{proofFile.FileName}' غير مسموح به. يرجى رفع ملف بصيغة PDF أو صورة معتمدة.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            if (!_fileStorageService.IsAllowedFileSize(proofFile.Length))
            {
                TempData["ErrorMessage"] = "حجم ملف الإثبات يتجاوز 20 ميجابايت.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            try
            {
                using var stream = proofFile.OpenReadStream();
                var upload = await _fileStorageService.SaveFileAsync(stream, proofFile.FileName, proofFile.ContentType, "milestones");
                dto.AttachmentUrl = upload.FilePath;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"فشل رفع وثيقة إثبات الإنجاز: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }
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
