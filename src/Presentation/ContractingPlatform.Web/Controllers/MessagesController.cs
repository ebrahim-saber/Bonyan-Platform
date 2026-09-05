using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContractingPlatform.Application.DTOs.Chat;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Web.Models;

namespace ContractingPlatform.Web.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly IChatService _chatService;
    private readonly IContractService _contractService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;

    public MessagesController(
        IChatService chatService,
        IContractService contractService,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService)
    {
        _chatService = chatService;
        _contractService = contractService;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? contractId)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var conversations = await _chatService.GetUserConversationsAsync(userId);

        var viewModel = new ChatIndexViewModel
        {
            Conversations = conversations,
            CurrentUserId = userId,
            ActiveContractId = contractId ?? conversations.FirstOrDefault()?.ProjectContractId
        };

        if (viewModel.ActiveContractId.HasValue)
        {
            var contractRes = await _contractService.GetContractDetailsAsync(viewModel.ActiveContractId.Value, userId, User.IsInRole("Admin"));
            if (contractRes.Success)
            {
                viewModel.ActiveContract = contractRes.Data;
            }

            viewModel.ActiveMessages = await _chatService.GetContractChatHistoryAsync(viewModel.ActiveContractId.Value, userId);
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetChatHistory(int contractId)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var messages = await _chatService.GetContractChatHistoryAsync(contractId, userId);
        return Json(messages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(SendChatMessageDto dto, IFormFile? attachmentFile)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, message = "غير مصرح" });
        }

        if (attachmentFile != null && attachmentFile.Length > 0)
        {
            if (!_fileStorageService.IsAllowedExtension(attachmentFile.FileName))
            {
                return Json(new { success = false, message = "نوع الملف المرفق غير مسموح به" });
            }

            if (!_fileStorageService.IsAllowedFileSize(attachmentFile.Length))
            {
                return Json(new { success = false, message = "حجم الملف المرفق يتجاوز الحد الأقصى (20 ميجابايت)" });
            }

            try
            {
                using var stream = attachmentFile.OpenReadStream();
                var upload = await _fileStorageService.SaveFileAsync(stream, attachmentFile.FileName, attachmentFile.ContentType, "chat");
                dto.AttachmentUrl = upload.FilePath;
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"فشل رفع المرفق: {ex.Message}" });
            }
        }

        var result = await _chatService.SendMessageAsync(dto, userId);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int contractId)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _chatService.MarkConversationAsReadAsync(contractId, userId);
        return Json(result);
    }
}
