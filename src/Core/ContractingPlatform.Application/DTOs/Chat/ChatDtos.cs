namespace ContractingPlatform.Application.DTOs.Chat;

public class ChatMessageDto
{
    public int Id { get; set; }
    public int? ProjectContractId { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string ReceiverUserId { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FormattedTime { get; set; } = string.Empty;
    public bool IsMine { get; set; }
}

public class SendChatMessageDto
{
    public int? ProjectContractId { get; set; }
    public string ReceiverUserId { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
}

public class ChatConversationDto
{
    public int? ProjectContractId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string OtherUserId { get; set; } = string.Empty;
    public string OtherUserName { get; set; } = string.Empty;
    public string OtherUserRole { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public string LastMessageFormattedTime { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
}
