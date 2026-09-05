namespace ContractingPlatform.Application.DTOs.Notifications;

public class NotificationItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}

public class UnreadNotificationsSummaryDto
{
    public int UnreadCount { get; set; }
    public List<NotificationItemDto> RecentNotifications { get; set; } = new();
}
