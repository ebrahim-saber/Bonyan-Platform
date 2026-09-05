using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContractingPlatform.Application.DTOs.Payments;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Web.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserService _currentUserService;

    public PaymentsController(IPaymentService paymentService, ICurrentUserService currentUserService)
    {
        _paymentService = paymentService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserType.Client))]
    public async Task<IActionResult> Checkout(int contractId, int? milestoneId)
    {
        var clientProfileId = await _currentUserService.GetClientProfileIdAsync();
        if (!clientProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "يجب تسجيل الدخول كعميل لإتمام الدفع";
            return RedirectToAction("Login", "Account");
        }

        var result = await _paymentService.PrepareCheckoutAsync(contractId, milestoneId, clientProfileId.Value);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Details", "Contracts", new { id = contractId });
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = nameof(UserType.Client))]
    public async Task<IActionResult> Process(ProcessPaymentDto dto)
    {
        var clientProfileId = await _currentUserService.GetClientProfileIdAsync();
        if (!clientProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "تعذر التحقق من حساب العميل";
            return RedirectToAction("Login", "Account");
        }

        var result = await _paymentService.ProcessPaymentAsync(dto, clientProfileId.Value);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Checkout), new { contractId = dto.ProjectContractId, milestoneId = dto.MilestoneId });
        }

        TempData["SuccessMessage"] = "تم تأمين الدفعة وإيداعها في حساب الضمان البنكي المشترك بنجاح";
        return RedirectToAction(nameof(Receipt), new { reference = result.Data.TransactionReference });
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(string reference)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _paymentService.GetReceiptAsync(reference, userId, User.IsInRole("Admin"));
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("MyContracts", "Contracts");
        }

        return View(result.Data);
    }
}
