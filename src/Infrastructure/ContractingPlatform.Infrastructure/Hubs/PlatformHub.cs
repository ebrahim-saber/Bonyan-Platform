using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ContractingPlatform.Infrastructure.Hubs;

[Authorize]
public class PlatformHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Join a specific Project room
    public async Task JoinProjectRoom(int projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Project_{projectId}");
    }

    public async Task LeaveProjectRoom(int projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Project_{projectId}");
    }

    // Join a specific Contract room for direct client-contractor chat
    public async Task JoinContractRoom(int contractId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Contract_{contractId}");
    }

    public async Task LeaveContractRoom(int contractId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Contract_{contractId}");
    }

    // Typing notification inside contract
    public async Task NotifyTyping(int contractId, string userName)
    {
        await Clients.OthersInGroup($"Contract_{contractId}").SendAsync("UserTyping", new
        {
            ContractId = contractId,
            UserName = userName
        });
    }
}
