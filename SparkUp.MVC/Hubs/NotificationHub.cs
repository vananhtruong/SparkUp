using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SparkUp.MVC.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.Identity?.Name;

            if (!string.IsNullOrEmpty(userId))
            {
                // Join user to their personal notification group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                Console.WriteLine($"[NotificationHub] User {userName} (ID: {userId}) connected and joined notification group");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.Identity?.Name;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                Console.WriteLine($"[NotificationHub] User {userName} (ID: {userId}) disconnected from notification group");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Client can call this to mark notification as read
        public async Task MarkNotificationAsRead(int notificationId)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"[NotificationHub] Marking notification {notificationId} as read for user {userId}");
                
                // This will be handled by NotificationService
                // Just acknowledge the client for now
                await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationHub] Error marking notification as read: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "Failed to mark notification as read");
            }
        }

        // Client can call this to get latest notification count
        public async Task RequestNotificationCount()
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    await Clients.Caller.SendAsync("NotificationCount", 0);
                    return;
                }

                // This will be handled by calling NotificationService
                Console.WriteLine($"[NotificationHub] Notification count requested for user {userId}");
                await Clients.Caller.SendAsync("NotificationCountRequested", userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationHub] Error getting notification count: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "Failed to get notification count");
            }
        }
    }
}
