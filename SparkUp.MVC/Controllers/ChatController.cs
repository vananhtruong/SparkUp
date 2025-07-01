using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SparkUp.MVC.Hubs;
using SparkUp.MVC.Service;
using System.Security.Claims;

namespace SparkUp.MVC.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(IChatService chatService, IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _hubContext = hubContext;
        }

        // Customer views
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            
            if (userRole == "ADMIN")
            {
                return RedirectToAction("AdminDashboard");
            }

            var sessions = await _chatService.GetCustomerChatSessionsAsync(int.Parse(userId));
            return View(sessions);
        }

        [HttpGet]
        public async Task<IActionResult> Session(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var session = await _chatService.GetChatSessionAsync(id);
            if (session == null) return NotFound();

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            
            // Check access permissions
            if (userRole == "CUSTOMER" && session.CustomerId != int.Parse(userId))
            {
                return Forbid();
            }
            else if (userRole == "ADMIN" && session.AdminId != int.Parse(userId) && session.AdminId != null)
            {
                return Forbid();
            }

            // Mark messages as read
            await _chatService.MarkMessagesAsReadAsync(id, int.Parse(userId));

            return View(session);
        }

        // Admin views
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AdminDashboard()
        {
            var sessions = await _chatService.GetAllChatSessionsAsync();
            return View(sessions);
        }

        // API endpoints
        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var session = await _chatService.CreateChatSessionAsync(int.Parse(userId), request.Subject);
            
            return Json(new { success = true, sessionId = session.Id });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var message = await _chatService.SendMessageAsync(
                request.SessionId, 
                int.Parse(userId), 
                request.MessageText
            );

            // Send real-time notification to other participants
            await _hubContext.Clients.Group($"Chat_{request.SessionId}")
                .SendAsync("ReceiveMessage", message.SenderId, message.Sender.FullName, message.MessageText, message.SentAt);

            return Json(new { 
                success = true, 
                messageId = message.Id,
                senderName = message.Sender.FullName,
                sentAt = message.SentAt 
            });
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AssignAdmin([FromBody] AssignAdminRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var success = await _chatService.AssignAdminToChatAsync(request.SessionId, int.Parse(userId));
            
            if (success)
            {
                await _hubContext.Clients.Group($"Chat_{request.SessionId}")
                    .SendAsync("AdminJoined", userId, User.Identity?.Name);
            }

            return Json(new { success });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            // Only admin can change status or customer can close their own session
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "ADMIN" && request.Status != "Closed")
            {
                return Forbid();
            }

            var success = await _chatService.UpdateChatStatusAsync(request.SessionId, request.Status);
            
            if (success)
            {
                await _hubContext.Clients.Group($"Chat_{request.SessionId}")
                    .SendAsync("StatusUpdated", request.Status);
            }

            return Json(new { success });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var count = await _chatService.GetUnreadMessageCountAsync(int.Parse(userId));
            return Json(new { count });
        }
    }

    // Request models
    public class CreateSessionRequest
    {
        public string Subject { get; set; } = string.Empty;
    }

    public class SendMessageRequest
    {
        public int SessionId { get; set; }
        public string MessageText { get; set; } = string.Empty;
    }

    public class AssignAdminRequest
    {
        public int SessionId { get; set; }
    }

    public class UpdateStatusRequest
    {
        public int SessionId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}