using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SparkUp.MVC.Models;
using SparkUp.MVC.Service;
using System.Security.Claims;

namespace SparkUp.MVC.Controllers
{
    [Authorize]
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly IChatService _chatService;
        
        public ApiController(IChatService chatService)
        {
            _chatService = chatService;
        }
        
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [HttpGet("chat/rooms")]
        public async Task<IActionResult> GetChatRooms()
        {
            var userId = GetCurrentUserId();
            
            if (userId == 0)
            {
                return Unauthorized("User not authenticated");
            }
            
            try
            {
                var chatRooms = await _chatService.GetUserChatRoomsAsync(userId);
                var result = chatRooms.Select(cr => new 
                {
                    id = cr.Id,
                    taskId = cr.TaskId,
                    taskTitle = cr.TaskTitle,
                    customerName = cr.CustomerName,
                    workerName = cr.WorkerName,
                    adminName = cr.AdminName,
                    status = cr.Status.ToString(),
                    lastMessageAt = cr.LastMessageAt,
                    lastMessage = cr.LastMessage ?? "Chưa có tin nhắn",
                    unreadCount = cr.UnreadCount,
                    userRole = cr.UserRole
                }).ToList(); // Ensure result is a List
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving chat rooms: {ex.Message}");
            }
        }
        
        [HttpGet("chat/messages/{chatRoomId}")]
        public async Task<IActionResult> GetMessages(int chatRoomId, int page = 1, int pageSize = 50)
        {
            var userId = GetCurrentUserId();
            
            if (userId == 0)
            {
                return Unauthorized("User not authenticated");
            }
            
            // Check if user can access this chat room
            var canAccess = await _chatService.CanUserAccessChatAsync(chatRoomId, userId);
            if (!canAccess)
            {
                return Forbid();
            }
            
            try
            {
                var messages = await _chatService.GetChatMessagesAsync(chatRoomId, page, pageSize);
                var messageViewModels = new List<ChatMessageViewModel>();
                
                foreach (var m in messages)
                {
                    var senderRole = m.SenderId == userId
                        ? await _chatService.GetUserRoleInChatAsync(chatRoomId, userId)
                        : await _chatService.GetUserRoleInChatAsync(chatRoomId, m.SenderId ?? 0);
                        
                    messageViewModels.Add(new ChatMessageViewModel
                    {
                        Id = m.Id,
                        ChatRoomId = m.ChatRoomId,
                        SenderId = m.SenderId ?? 0,
                        SenderName = m.Sender?.FullName ?? "Unknown",
                        Content = m.Content,
                        SentAt = m.SentAt,
                        IsRead = m.IsRead,
                        IsDeleted = m.IsDeleted,
                        MessageType = m.MessageType,
                        IsOwnMessage = m.SenderId == userId,
                        SenderRole = senderRole
                    });
                }
                
                return Ok(messageViewModels);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving messages: {ex.Message}");
            }
        }
        
        [HttpPost("chat/rooms/{chatRoomId}/markAllRead")]
        public async Task<IActionResult> MarkAllAsRead(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            
            if (userId == 0)
            {
                return Unauthorized("User not authenticated");
            }
            
            // Check if user can access this chat room
            var canAccess = await _chatService.CanUserAccessChatAsync(chatRoomId, userId);
            if (!canAccess)
            {
                return Forbid();
            }
            
            try
            {
                await _chatService.MarkAllMessagesAsReadAsync(chatRoomId, userId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error marking messages as read: {ex.Message}");
            }
        }
        
        [HttpPost("chat/messages/{messageId}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int messageId)
        {
            var userId = GetCurrentUserId();
            
            if (userId == 0)
            {
                return Unauthorized("User not authenticated");
            }
            
            var message = await _chatService.GetMessageByIdAsync(messageId);
            if (message == null)
            {
                return NotFound();
            }

            // Check if user can access the chat room that contains this message
            var canAccess = await _chatService.CanUserAccessChatAsync(message.ChatRoomId, userId);
            if (!canAccess)
            {
                return Forbid();
            }

            var success = await _chatService.MarkMessageAsReadAsync(messageId, userId);
            return Ok(new { success });
        }
    }
}
