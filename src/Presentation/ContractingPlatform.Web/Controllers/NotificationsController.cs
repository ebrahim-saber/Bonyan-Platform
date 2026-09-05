using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContractingPlatform.Application.Interfaces;

namespace ContractingPlatform.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(INotificationService notificationService, ICurrentUserService currentUserService)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var notifications = await _notificationService.GetUserNotificationsAsync(userId, page, 30);
        return View(notifications);
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadSummary()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { unreadCount = 0, recentNotifications = Array.Empty<object>() });
        }

        var summary = await _notificationService.GetUnreadSummaryAsync(userId);
        return Json(summary);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, message = "غير مصرح" });
        }

        var result = await _notificationService.MarkAsReadAsync(id, userId);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, message = "غير مصرح" });
        }

        var result = await _notificationService.MarkAllAsReadAsync(userId);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(result);
        }

        TempData["SuccessMessage"] = "تم تحديد كافة الإشعارات كمقروءة";
        return RedirectToAction(nameof(Index));
    }
}
