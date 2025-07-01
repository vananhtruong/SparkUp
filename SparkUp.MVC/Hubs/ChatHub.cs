using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SparkUp.MVC.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public async Task JoinChatSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{sessionId}");
        }

        public async Task LeaveChatSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Chat_{sessionId}");
        }

        public async Task SendMessage(string sessionId, string message)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.Identity?.Name ?? "Unknown User";

            await Clients.Group($"Chat_{sessionId}").SendAsync("ReceiveMessage", userId, userName, message, DateTime.UtcNow);
        }

        public async Task UpdateOnlineStatus(string sessionId, bool isOnline)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = Context.User?.FindFirstValue(ClaimTypes.Role);

            await Clients.Group($"Chat_{sessionId}").SendAsync("UpdateOnlineStatus", userId, userRole, isOnline);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            await Clients.All.SendAsync("UserConnected", userId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            await Clients.All.SendAsync("UserDisconnected", userId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}