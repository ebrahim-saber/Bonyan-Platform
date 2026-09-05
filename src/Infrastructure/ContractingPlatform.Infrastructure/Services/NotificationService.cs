using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application.DTOs.Common;
using ContractingPlatform.Application.DTOs.Notifications;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Infrastructure.Data;
using ContractingPlatform.Infrastructure.Hubs;

namespace ContractingPlatform.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<PlatformHub> _hubContext;

    public NotificationService(ApplicationDbContext context, IHubContext<PlatformHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(string userId, string title, string message, string? actionUrl = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var dto = new NotificationItemDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            ActionUrl = notification.ActionUrl,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            TimeAgo = "الآن"
        };

        // Send via User ID & User Group
        await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", dto);
        await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", dto);
    }

    public async Task<List<NotificationItemDto>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20)
    {
        var items = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return items.Select(n => new NotificationItemDto
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            ActionUrl = n.ActionUrl,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            TimeAgo = GetTimeAgo(n.CreatedAt)
        }).ToList();
    }

    public async Task<UnreadNotificationsSummaryDto> GetUnreadSummaryAsync(string userId)
    {
        var unreadCount = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();

        var recentItems = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .ToListAsync();

        return new UnreadNotificationsSummaryDto
        {
            UnreadCount = unreadCount,
            RecentNotifications = recentItems.Select(n => new NotificationItemDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                TimeAgo = GetTimeAgo(n.CreatedAt)
            }).ToList()
        };
    }

    public async Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId, string userId)
    {
        var item = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (item == null)
        {
            return ApiResponse<bool>.Fail("الإشعار غير موجود");
        }

        if (!item.IsRead)
        {
            item.IsRead = true;
            item.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return ApiResponse<bool>.Ok(true, "تم تحديد الإشعار كمقروء");
    }

    public async Task<ApiResponse<bool>> MarkAllAsReadAsync(string userId)
    {
        var unreadItems = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unreadItems.Any())
        {
            var now = DateTime.UtcNow;
            foreach (var item in unreadItems)
            {
                item.IsRead = true;
                item.ReadAt = now;
            }
            await _context.SaveChangesAsync();
        }

        return ApiResponse<bool>.Ok(true, "تم تحديد كافة الإشعارات كمقروءة");
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;
        if (span.TotalMinutes < 1) return "الآن";
        if (span.TotalMinutes < 60) return $"منذ {Math.Max(1, (int)span.TotalMinutes)} دقيقة";
        if (span.TotalHours < 24) return $"منذ {(int)span.TotalHours} ساعة";
        if (span.TotalDays < 30) return $"منذ {(int)span.TotalDays} يوم";
        return dateTime.ToString("yyyy/MM/dd");
    }
}
