using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application.DTOs.Common;
using ContractingPlatform.Application.DTOs.Chat;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Domain.Enums;
using ContractingPlatform.Infrastructure.Data;
using ContractingPlatform.Infrastructure.Hubs;

namespace ContractingPlatform.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<PlatformHub> _hubContext;
    private readonly INotificationService _notificationService;

    public ChatService(
        ApplicationDbContext context,
        IHubContext<PlatformHub> hubContext,
        INotificationService notificationService)
    {
        _context = context;
        _hubContext = hubContext;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<ChatMessageDto>> SendMessageAsync(SendChatMessageDto dto, string senderUserId)
    {
        if (string.IsNullOrWhiteSpace(dto.MessageText) && string.IsNullOrEmpty(dto.AttachmentUrl))
        {
            return ApiResponse<ChatMessageDto>.Fail("لا يمكن إرسال رسالة فارغة");
        }

        var sender = await _context.Users.FindAsync(senderUserId);
        if (sender == null)
        {
            return ApiResponse<ChatMessageDto>.Fail("المستخدم المرسل غير صالح");
        }

        string receiverUserId = dto.ReceiverUserId;
        string projectTitle = "محادثة مباشرة";

        if (dto.ProjectContractId.HasValue)
        {
            var contract = await _context.ProjectContracts
                .Include(c => c.Client).ThenInclude(cl => cl.User)
                .Include(c => c.Contractor).ThenInclude(co => co.User)
                .Include(c => c.ProjectRequest)
                .FirstOrDefaultAsync(c => c.Id == dto.ProjectContractId.Value);

            if (contract == null)
            {
                return ApiResponse<ChatMessageDto>.Fail("العقد المرتبط بالمحادثة غير موجود");
            }

            projectTitle = contract.ProjectRequest.Title;

            // Ensure sender is a party to this contract
            bool isClient = contract.Client.UserId == senderUserId;
            bool isContractor = contract.Contractor.UserId == senderUserId;

            if (!isClient && !isContractor)
            {
                return ApiResponse<ChatMessageDto>.Fail("غير مصرح لك بالإرسال في محادثة هذا العقد");
            }

            // If receiver not provided, determine recipient
            if (string.IsNullOrEmpty(receiverUserId))
            {
                receiverUserId = isClient ? contract.Contractor.UserId : contract.Client.UserId;
            }
        }

        if (string.IsNullOrEmpty(receiverUserId))
        {
            return ApiResponse<ChatMessageDto>.Fail("لم يتم تحديد المستخدم المستلم");
        }

        var receiver = await _context.Users.FindAsync(receiverUserId);
        if (receiver == null)
        {
            return ApiResponse<ChatMessageDto>.Fail("المستلم غير موجود بالنظام");
        }

        var message = new ChatMessage
        {
            ProjectContractId = dto.ProjectContractId,
            SenderUserId = senderUserId,
            ReceiverUserId = receiverUserId,
            MessageText = dto.MessageText.Trim(),
            AttachmentUrl = dto.AttachmentUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        var messageDto = new ChatMessageDto
        {
            Id = message.Id,
            ProjectContractId = message.ProjectContractId,
            SenderUserId = senderUserId,
            SenderName = sender.FullName,
            SenderRole = sender.UserType == UserType.Contractor ? "المقاول" : "العميل",
            ReceiverUserId = receiverUserId,
            ReceiverName = receiver.FullName,
            MessageText = message.MessageText,
            AttachmentUrl = message.AttachmentUrl,
            IsRead = false,
            CreatedAt = message.CreatedAt,
            FormattedTime = message.CreatedAt.ToLocalTime().ToString("hh:mm tt"),
            IsMine = true
        };

        // Broadcast to SignalR contract room if applicable
        if (dto.ProjectContractId.HasValue)
        {
            await _hubContext.Clients.Group($"Contract_{dto.ProjectContractId.Value}").SendAsync("ReceiveChatMessage", messageDto);
        }

        // Also broadcast directly to receiver's private channel
        await _hubContext.Clients.User(receiverUserId).SendAsync("ReceiveChatMessage", messageDto);
        await _hubContext.Clients.Group($"User_{receiverUserId}").SendAsync("ReceiveChatMessage", messageDto);

        // Send a notification alert to recipient
        string snippet = message.MessageText.Length > 60 ? message.MessageText.Substring(0, 57) + "..." : message.MessageText;
        string actionUrl = dto.ProjectContractId.HasValue 
            ? $"/Messages?contractId={dto.ProjectContractId.Value}" 
            : "/Messages";

        await _notificationService.SendNotificationAsync(
            receiverUserId,
            $"رسالة جديدة من {sender.FullName}",
            snippet,
            actionUrl);

        return ApiResponse<ChatMessageDto>.Ok(messageDto, "تم إرسال الرسالة بنجاح");
    }

    public async Task<List<ChatMessageDto>> GetContractChatHistoryAsync(int contractId, string currentUserId)
    {
        var messages = await _context.ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.ProjectContractId == contractId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        // Mark incoming messages as read
        var unreadIncoming = messages.Where(m => m.ReceiverUserId == currentUserId && !m.IsRead).ToList();
        if (unreadIncoming.Any())
        {
            var now = DateTime.UtcNow;
            foreach (var msg in unreadIncoming)
            {
                msg.IsRead = true;
                msg.ReadAt = now;
            }
            await _context.SaveChangesAsync();
        }

        return messages.Select(m => new ChatMessageDto
        {
            Id = m.Id,
            ProjectContractId = m.ProjectContractId,
            SenderUserId = m.SenderUserId,
            SenderName = m.Sender?.FullName ?? "مستخدم",
            SenderRole = m.Sender?.UserType == UserType.Contractor ? "المقاول" : "العميل",
            ReceiverUserId = m.ReceiverUserId,
            ReceiverName = m.Receiver?.FullName ?? "مستخدم",
            MessageText = m.MessageText,
            AttachmentUrl = m.AttachmentUrl,
            IsRead = m.IsRead,
            CreatedAt = m.CreatedAt,
            FormattedTime = m.CreatedAt.ToLocalTime().ToString("hh:mm tt"),
            IsMine = m.SenderUserId == currentUserId
        }).ToList();
    }

    public async Task<List<ChatConversationDto>> GetUserConversationsAsync(string currentUserId)
    {
        // Find all contracts where the user is either Client or Contractor
        var contracts = await _context.ProjectContracts
            .Include(c => c.ProjectRequest)
            .Include(c => c.Client).ThenInclude(cl => cl.User)
            .Include(c => c.Contractor).ThenInclude(co => co.User)
            .Include(c => c.ChatMessages)
            .Where(c => c.Client.UserId == currentUserId || c.Contractor.UserId == currentUserId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var result = new List<ChatConversationDto>();

        foreach (var c in contracts)
        {
            bool isClient = c.Client.UserId == currentUserId;
            var otherUser = isClient ? c.Contractor.User : c.Client.User;
            var otherRole = isClient ? "المقاول المنفذ" : "صاحب المشروع";

            var lastMessage = c.ChatMessages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            var unreadCount = c.ChatMessages.Count(m => m.ReceiverUserId == currentUserId && !m.IsRead);

            result.Add(new ChatConversationDto
            {
                ProjectContractId = c.Id,
                ProjectTitle = c.ProjectRequest?.Title ?? $"عقد رقم #{c.Id}",
                OtherUserId = otherUser?.Id ?? "",
                OtherUserName = otherUser?.FullName ?? (isClient ? c.Contractor.CompanyName : "العميل"),
                OtherUserRole = otherRole,
                LastMessage = lastMessage?.MessageText ?? "لا توجد رسائل سابقة",
                LastMessageTime = lastMessage?.CreatedAt ?? c.CreatedAt,
                LastMessageFormattedTime = lastMessage != null ? lastMessage.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd hh:mm tt") : c.CreatedAt.ToString("yyyy/MM/dd"),
                UnreadCount = unreadCount
            });
        }

        return result.OrderByDescending(x => x.LastMessageTime).ToList();
    }

    public async Task<ApiResponse<bool>> MarkConversationAsReadAsync(int contractId, string currentUserId)
    {
        var unread = await _context.ChatMessages
            .Where(m => m.ProjectContractId == contractId && m.ReceiverUserId == currentUserId && !m.IsRead)
            .ToListAsync();

        if (unread.Any())
        {
            var now = DateTime.UtcNow;
            foreach (var m in unread)
            {
                m.IsRead = true;
                m.ReadAt = now;
            }
            await _context.SaveChangesAsync();
        }

        return ApiResponse<bool>.Ok(true, "تم تحديد الرسائل كمقروءة");
    }
}
