using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SparkUp.MVC.Service;
using SparkUp.Business;

namespace SparkUp.MVC.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }        public async System.Threading.Tasks.Task JoinChatRoom(string chatRoomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
            
            // Notify others that user joined
            await Clients.Group($"ChatRoom_{chatRoomId}")
                .SendAsync("UserJoined", Context.User?.Identity?.Name);
        }

        public async System.Threading.Tasks.Task LeaveChatRoom(string chatRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
            
            // Notify others that user left
            await Clients.Group($"ChatRoom_{chatRoomId}")
                .SendAsync("UserLeft", Context.User?.Identity?.Name);
        }        public async System.Threading.Tasks.Task SendMessage(string chatRoomId, string message)
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            var userName = Context.User?.Identity?.Name;

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || string.IsNullOrEmpty(message?.Trim()))
                return;

            try
            {
                // Check if user can access this chat room
                var canAccess = await _chatService.CanUserAccessChatAsync(int.Parse(chatRoomId), userId);
                if (!canAccess)
                {
                    await Clients.Caller.SendAsync("Error", "You don't have access to this chat room");
                    return;
                }

                // Save message to database using ChatService
                var savedMessage = await _chatService.SendMessageAsync(
                    int.Parse(chatRoomId), 
                    userId, 
                    message.Trim(), 
                    ChatMessageType.Text);

                // Send message to all users in the chat room
                await Clients.Group($"ChatRoom_{chatRoomId}")
                    .SendAsync("ReceiveMessage", new
                    {
                        Id = savedMessage.Id,
                        SenderId = savedMessage.SenderId,
                        SenderName = savedMessage.Sender?.FullName ?? userName,
                        Content = savedMessage.Content,
                        SentAt = savedMessage.SentAt,
                        MessageType = savedMessage.MessageType.ToString(),
                        IsOwnMessage = false // Will be determined on client side
                    });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", $"Failed to send message: {ex.Message}");
            }
        }        public async System.Threading.Tasks.Task SendTypingIndicator(string chatRoomId, bool isTyping)
        {
            var userName = Context.User?.Identity?.Name;
            
            // Send typing indicator to others in the room (except sender)
            await Clients.GroupExcept($"ChatRoom_{chatRoomId}", Context.ConnectionId)
                .SendAsync("UserTyping", new { UserName = userName, IsTyping = isTyping });
        }

        public async System.Threading.Tasks.Task UserTyping(object data)
        {
            try
            {
                // Extract data from the object
                var chatRoomId = data.GetType().GetProperty("chatRoomId")?.GetValue(data)?.ToString();
                var userId = data.GetType().GetProperty("userId")?.GetValue(data)?.ToString();
                var userName = data.GetType().GetProperty("userName")?.GetValue(data)?.ToString();
                var isTyping = data.GetType().GetProperty("isTyping")?.GetValue(data);

                if (string.IsNullOrEmpty(chatRoomId))
                    return;

                // Notify other users in the group about typing status
                await Clients.OthersInGroup($"ChatRoom_{chatRoomId}")
                    .SendAsync("UserTyping", new
                    {
                        userId = userId,
                        userName = userName,
                        isTyping = isTyping
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] UserTyping failed: {ex.Message}");
            }
        }

        public async System.Threading.Tasks.Task MarkMessageAsRead(object data)
        {
            try
            {
                var messageId = data.GetType().GetProperty("messageId")?.GetValue(data)?.ToString();
                var chatRoomId = data.GetType().GetProperty("chatRoomId")?.GetValue(data)?.ToString();
                var userId = data.GetType().GetProperty("userId")?.GetValue(data)?.ToString();

                if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(chatRoomId))
                    return;

                // Notify other users in the group about message read status
                await Clients.OthersInGroup($"ChatRoom_{chatRoomId}")
                    .SendAsync("MessageRead", new
                    {
                        messageId = messageId,
                        readBy = userId
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] MarkMessageAsRead failed: {ex.Message}");
            }
        }

        public override async System.Threading.Tasks.Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.Identity?.Name;
            
            Console.WriteLine($"User {userName} (ID: {userId}) connected to ChatHub");
            await base.OnConnectedAsync();
        }

        public override async System.Threading.Tasks.Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.Identity?.Name;
            
            Console.WriteLine($"User {userName} (ID: {userId}) disconnected from ChatHub");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
