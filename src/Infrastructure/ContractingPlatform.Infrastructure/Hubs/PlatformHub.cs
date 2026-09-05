using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ContractingPlatform.Infrastructure.Hubs;

[Authorize]
public class PlatformHub : Hub
{
    // Join a specific Project/Contract room for real-time chat & updates
    public async Task JoinProjectRoom(int projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Project_{projectId}");
    }

    public async Task LeaveProjectRoom(int projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Project_{projectId}");
    }

    // Send a message within a project discussion
    public async Task SendMessage(int projectId, string senderName, string message)
    {
        await Clients.Group($"Project_{projectId}").SendAsync("ReceiveMessage", new
        {
            ProjectId = projectId,
            SenderName = senderName,
            Message = message,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")
        });
    }
}
