using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SparkUp.Business;
using SparkUp.MVC.Hubs;
using SparkUp.MVC.Models;
using SparkUp.MVC.Service;
using System.Security.Claims;

namespace SparkUp.MVC.Controllers
{
    [Authorize]    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly INotificationService _notificationService;

        public ChatController(IChatService chatService, IHubContext<ChatHub> hubContext, INotificationService notificationService)
        {
            _chatService = chatService;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }        // GET: Chat/Index - List all chat rooms for current user
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            
            // Check if user is authenticated
            if (userId == 0)
            {
                return RedirectToAction("Index", "Authentication");
            }
            
            var chatRooms = new List<ChatRoomViewModel>();

            // This would need to be implemented - get all chat rooms where user is participant
            // For now, return empty list
            return View(chatRooms);
        }        // GET: Chat/Room/{chatRoomId} - Display chat room
        public async Task<IActionResult> Room(int id)
        {
            var userId = GetCurrentUserId();
            
            // Check if user is authenticated
            if (userId == 0)
            {
                return RedirectToAction("Index", "Authentication");
            }
            
            // Check if user can access this chat room
            var canAccess = await _chatService.CanUserAccessChatAsync(id, userId);
            if (!canAccess)
            {
                return Forbid();
            }

            var chatRoom = await _chatService.GetChatRoomByIdAsync(id);
            if (chatRoom == null)
            {
                return NotFound();
            }

            // Mark all messages as read for current user
            await _chatService.MarkAllMessagesAsReadAsync(id, userId);

            var viewModel = new ChatRoomViewModel
            {
                Id = chatRoom.Id,
                TaskId = chatRoom.TaskId,
                TaskTitle = chatRoom.Task?.Description ?? "",
                CustomerName = chatRoom.Customer?.FullName ?? "",
                WorkerName = chatRoom.Worker?.FullName ?? "",
                AdminName = chatRoom.AdminUser?.FullName,
                Status = chatRoom.Status,
                DisputeReason = chatRoom.DisputeReason,
                CreatedAt = chatRoom.CreatedAt,
                DisputedAt = chatRoom.DisputedAt,
                ResolvedAt = chatRoom.ResolvedAt,
                LastMessageAt = chatRoom.LastMessageAt,
                UserRole = await _chatService.GetUserRoleInChatAsync(id, userId),
                CanAccess = true
            };

            return View(viewModel);
        }        // GET: Chat/ForTask/{taskId} - Get or create chat room for task
        public async Task<IActionResult> ForTask(int taskId)
        {
            var userId = GetCurrentUserId();
            
            // Check if user is authenticated
            if (userId == 0)
            {
                return RedirectToAction("Index", "Authentication");
            }
            
            // Check if user can access chat for this task
            var canAccess = await _chatService.IsChatAvailableForTaskAsync(taskId, userId);
            if (!canAccess)
            {
                return Forbid("Chat is not available for this task or you don't have access.");
            }

            // Get or create chat room
            var chatRoom = await _chatService.GetOrCreateChatRoomAsync(taskId);
            if (chatRoom == null)
            {
                return BadRequest("Unable to create or access chat room.");
            }

            return RedirectToAction("Room", new { id = chatRoom.Id });
        }        // API Endpoints        // GET: Chat/Api/Messages/{chatRoomId} or Chat/GetMessages?chatRoomId=X or Chat/GetMessages/{chatRoomId}
        [HttpGet]
        [Route("Api/Messages/{chatRoomId}")]
        [Route("GetMessages")]
        [Route("GetMessages/{chatRoomId}")]
        [Route("Chat/GetMessages/{chatRoomId}")]
        [Route("Chat/Api/Messages/{chatRoomId}")]
        [Route("Chat/GetMessages")]        public async Task<IActionResult> GetMessages([FromRoute] int? chatRoomId = null, [FromQuery] int? queryChatRoomId = null, int page = 1, int pageSize = 50)
        {
            // Allow chatRoomId to be specified in either route or query string
            int actualChatRoomId = chatRoomId ?? queryChatRoomId ?? 0;
            
            // Add debugging logs
            Console.WriteLine($"[DEBUG] GetMessages called - chatRoomId: {chatRoomId}, queryChatRoomId: {queryChatRoomId}, actualChatRoomId: {actualChatRoomId}");
            
            if (actualChatRoomId <= 0)
            {
                Console.WriteLine("[DEBUG] GetMessages failed - Chat room ID is required");
                return BadRequest("Chat room ID is required");
            }
            var userId = GetCurrentUserId();
            
            Console.WriteLine($"[DEBUG] GetMessages - userId: {userId}");
            
            // Check if user is authenticated
            if (userId == 0)
            {
                Console.WriteLine("[DEBUG] GetMessages failed - User not authenticated");
                return Unauthorized("User not authenticated");
            }
              var canAccess = await _chatService.CanUserAccessChatAsync(actualChatRoomId, userId);
            if (!canAccess)
            {
                Console.WriteLine("[DEBUG] GetMessages failed - User cannot access this chat room");
                return Forbid();
            }

            Console.WriteLine($"[DEBUG] GetMessages - Loading messages for room {actualChatRoomId}");
            var messages = await _chatService.GetChatMessagesAsync(actualChatRoomId, page, pageSize);
            Console.WriteLine($"[DEBUG] GetMessages - Found {messages.Count()} messages");
            // Fix for CS4034: The 'await' operator can only be used within an async lambda expression.
            // The issue occurs because the lambda expression in the LINQ `Select` statement is not marked as `async`.
            // To fix this, mark the lambda as `async` and ensure the method is properly awaited.

            var messageViewModels = await System.Threading.Tasks.Task.WhenAll(messages.Select(async m => new ChatMessageViewModel
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
                SenderRole = m.SenderId == userId
                    ? await _chatService.GetUserRoleInChatAsync(actualChatRoomId, userId)
                    : await _chatService.GetUserRoleInChatAsync(actualChatRoomId, m.SenderId ?? 0)            }));

            Console.WriteLine($"[DEBUG] GetMessages - Returning {messageViewModels.Length} message view models");
            return Json(messageViewModels);
        }        // POST: Chat/SendMessage or Chat/Api/SendMessage        [HttpPost]
        [Route("SendMessage")]  // Default route which matches the client-side request
        [Route("Api/SendMessage")] // Also keep the API route for consistency
        [Route("Chat/SendMessage")] // Add explicit path to match client-side request
        [Route("Chat/Api/SendMessage")] // Add explicit path with prefix
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            Console.WriteLine($"[DEBUG] SendMessage called");
            Console.WriteLine($"[DEBUG] Request object: {request != null}");
            Console.WriteLine($"[DEBUG] Request type: {request?.GetType()?.Name ?? "null"}");
            
            // Try to read the raw request body for debugging
            try
            {
                Request.Body.Position = 0;
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();
                Console.WriteLine($"[DEBUG] Raw request body: '{requestBody}'");
                Request.Body.Position = 0; // Reset for model binding
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error reading request body: {ex.Message}");
            }
            
            if (request == null)
            {
                Console.WriteLine("[DEBUG] SendMessage failed - Request is null");
                Console.WriteLine($"[DEBUG] Content-Type: {Request.ContentType}");
                Console.WriteLine($"[DEBUG] Content-Length: {Request.ContentLength}");
                Console.WriteLine($"[DEBUG] Method: {Request.Method}");
                return BadRequest("Request data is required.");
            }

            Console.WriteLine($"[DEBUG] SendMessage - ChatRoomId: {request.ChatRoomId}, Content: '{request.Content}', MessageType: {request.MessageType}");

            var userId = GetCurrentUserId();
            
            // Check if user is authenticated
            if (userId == 0)
            {
                Console.WriteLine("[DEBUG] SendMessage failed - User not authenticated");
                return Unauthorized("User not authenticated");
            }
            
            // Validate required fields
            if (request.ChatRoomId <= 0)
            {
                Console.WriteLine("[DEBUG] SendMessage failed - Invalid ChatRoomId");
                return BadRequest("Valid ChatRoomId is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                Console.WriteLine("[DEBUG] SendMessage failed - Empty content");
                return BadRequest("Message content cannot be empty.");
            }
              var canAccess = await _chatService.CanUserAccessChatAsync(request.ChatRoomId, userId);
            if (!canAccess)
            {
                Console.WriteLine($"[DEBUG] SendMessage failed - User {userId} cannot access chat room {request.ChatRoomId}");
                return Forbid();
            }

            try
            {
                // Log request details for debugging
                Console.WriteLine($"Processing message: ChatRoomId={request.ChatRoomId}, UserId={userId}, Content Length={request.Content?.Length ?? 0}");

                // Ensure content isn't null or just whitespace
                string safeContent = request.Content?.Trim() ?? string.Empty;                if (string.IsNullOrEmpty(safeContent))
                {
                    Console.WriteLine("Message rejected: Content is empty after trimming");
                    return BadRequest("Message content cannot be empty.");
                }                Console.WriteLine("Calling ChatService.SendMessageAsync...");
                // Set default message type if not provided - handle nullable properly
                var messageType = request.MessageType.HasValue ? request.MessageType.Value : SparkUp.Business.ChatMessageType.Text;
                
                var message = await _chatService.SendMessageAsync(
                    request.ChatRoomId, 
                    userId, 
                    safeContent, 
                    messageType);
                  // Log the result
                Console.WriteLine($"Message created successfully with ID: {message.Id}");
                
                // Send real-time message via SignalR
                await _hubContext.Clients.Group($"ChatRoom_{request.ChatRoomId}")
                    .SendAsync("ReceiveMessage", new
                    {
                        Id = message.Id,
                        SenderId = message.SenderId,
                        SenderName = message.Sender?.FullName ?? "Unknown",
                        Content = message.Content,
                        SentAt = message.SentAt,
                        MessageType = message.MessageType.ToString(),
                        IsOwnMessage = false // Will be determined on client side
                    });

                // Send notification to other chat participants
                try
                {
                    var chatRoom = await _chatService.GetChatRoomByIdAsync(request.ChatRoomId);
                    if (chatRoom?.Task != null)
                    {
                        var senderName = message.Sender?.FullName ?? "Unknown";
                        var messagePreview = safeContent.Length > 50 ? safeContent.Substring(0, 50) + "..." : safeContent;

                        // Notify customer if sender is worker
                        if (chatRoom.Task.WorkerId == userId && chatRoom.Task.CustomerId != userId)
                        {
                            await _notificationService.SendChatNotificationAsync(
                                chatRoom.Task.CustomerId, userId, senderName, request.ChatRoomId, messagePreview);
                        }
                        // Notify worker if sender is customer
                        else if (chatRoom.Task.CustomerId == userId && chatRoom.Task.WorkerId != userId)
                        {
                            await _notificationService.SendChatNotificationAsync(
                                chatRoom.Task.WorkerId, userId, senderName, request.ChatRoomId, messagePreview);
                        }
                        // Notify admin if present
                        if (chatRoom.AdminUserId.HasValue && chatRoom.AdminUserId.Value != userId)
                        {
                            await _notificationService.SendChatNotificationAsync(
                                chatRoom.AdminUserId.Value, userId, senderName, request.ChatRoomId, messagePreview);
                        }
                    }
                }
                catch (Exception notificationEx)
                {
                    Console.WriteLine($"Failed to send chat notification: {notificationEx.Message}");
                    // Don't fail the message sending if notification fails
                }

                return Json(new { success = true, messageId = message.Id });
            }
            catch (Exception ex)
            {
                // Log the error details
                Console.WriteLine($"Error sending message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return BadRequest($"Error sending message: {ex.Message}");
            }
        }// POST: Chat/Api/InitiateDispute
        [HttpPost]
        public async Task<IActionResult> InitiateDispute([FromBody] InitiateDisputeRequest request)
        {
            var userId = GetCurrentUserId();
            
            // Check if user is authenticated
            if (userId == 0)
            {
                return Unauthorized("User not authenticated");
            }
            
            var canAccess = await _chatService.CanUserAccessChatAsync(request.ChatRoomId, userId);
            if (!canAccess)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest("Dispute reason is required.");
            }

            try
            {
                var chatRoom = await _chatService.InitiateDisputeAsync(
                    request.ChatRoomId, 
                    userId, 
                    request.Reason);

                if (chatRoom == null)
                {
                    return BadRequest("Unable to initiate dispute. Chat room may already be disputed.");
                }

                // Notify all participants via SignalR
                await _hubContext.Clients.Group($"ChatRoom_{request.ChatRoomId}")
                    .SendAsync("DisputeInitiated", new
                    {
                        ChatRoomId = request.ChatRoomId,
                        Reason = request.Reason,
                        InitiatedBy = User.Identity?.Name
                    });

                return Json(new { success = true, status = chatRoom.Status.ToString() });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error initiating dispute: {ex.Message}");
            }        }        // POST: Chat/MarkAsRead
        [HttpPost]
        [Route("MarkAsRead")]
        [Route("Chat/MarkAsRead")]
        [Route("MarkAsRead/{messageId}")]
        [Route("Chat/MarkAsRead/{messageId}")]
        public async Task<IActionResult> MarkAsRead([FromBody] int? bodyMessageId = null, [FromRoute] int? messageId = null)
        {
            var actualMessageId = bodyMessageId ?? messageId ?? 0;
            
            Console.WriteLine($"[DEBUG] MarkAsRead called - bodyMessageId: {bodyMessageId}, routeMessageId: {messageId}, actualMessageId: {actualMessageId}");
            
            if (actualMessageId <= 0)
            {
                Console.WriteLine("[DEBUG] MarkAsRead failed - Invalid message ID");
                return BadRequest("Valid message ID is required");
            }
            
            var userId = GetCurrentUserId();
            
            // Check if user is authenticated
            if (userId == 0)
            {
                Console.WriteLine("[DEBUG] MarkAsRead failed - User not authenticated");
                return Unauthorized("User not authenticated");
            }
            
            try
            {
                var message = await _chatService.GetMessageByIdAsync(actualMessageId);
                if (message == null)
                {
                    Console.WriteLine($"[DEBUG] MarkAsRead failed - Message {actualMessageId} not found");
                    return NotFound();
                }

                var canAccess = await _chatService.CanUserAccessChatAsync(message.ChatRoomId, userId);
                if (!canAccess)
                {
                    Console.WriteLine($"[DEBUG] MarkAsRead failed - User {userId} cannot access chat room {message.ChatRoomId}");
                    return Forbid();
                }

                var success = await _chatService.MarkMessageAsReadAsync(actualMessageId, userId);
                
                Console.WriteLine($"[DEBUG] MarkAsRead result: {success}");
                
                if (success)
                {
                    // Notify sender via SignalR
                    await _hubContext.Clients.Group($"ChatRoom_{message.ChatRoomId}")
                        .SendAsync("MessageRead", new { MessageId = actualMessageId, ReadBy = userId });
                }

                return Json(new { success });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] MarkAsRead exception: {ex.Message}");
                return BadRequest(new { error = "Failed to mark message as read", details = ex.Message });
            }
        }        // POST: Chat/MarkAllAsRead/{chatRoomId} - Mark all messages in a chat room as read
        [HttpPost]
        [Route("MarkAllAsRead/{chatRoomId}")]
        [Route("Chat/MarkAllAsRead/{chatRoomId}")]
        public async Task<IActionResult> MarkAllAsRead(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            
            // Check if user is authenticated
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
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error marking messages as read: {ex.Message}");
            }
        }
        
        // GET: Chat/Api/UnreadCount/{chatRoomId}
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            
            // Check if user is authenticated
            if (userId == 0)
            {
                return Unauthorized("User not authenticated");
            }
            
            var canAccess = await _chatService.CanUserAccessChatAsync(chatRoomId, userId);
            if (!canAccess)
            {
                return Forbid();
            }

            var count = await _chatService.GetUnreadMessageCountAsync(chatRoomId, userId);
            return Json(new { unreadCount = count });
        }

        // Admin-only endpoints

        // GET: Chat/Admin/Disputes - List pending disputes
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PendingDisputes()
        {
            var disputes = await _chatService.GetPendingDisputesAsync();
            return View(disputes);
        }

        // POST: Chat/Admin/AssignAdmin
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignAdmin([FromBody] object request)
        {
            // Implementation for admin assignment
            return Json(new { success = true });
        }

        // POST: Chat/Admin/ResolveDispute
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResolveDispute([FromBody] ResolveDisputeRequest request)
        {
            var userId = GetCurrentUserId();

            try
            {
                var resolution = await _chatService.ResolveDisputeAsync(
                    request.ChatRoomId,
                    userId,
                    request.Resolution,
                    request.ActionTaken,
                    request.RefundAmount,
                    request.Notes);

                // Notify all participants via SignalR
                await _hubContext.Clients.Group($"ChatRoom_{request.ChatRoomId}")
                    .SendAsync("DisputeResolved", new
                    {
                        ChatRoomId = request.ChatRoomId,
                        Resolution = request.Resolution,
                        ActionTaken = request.ActionTaken,
                        ResolvedBy = User.Identity?.Name
                    });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error resolving dispute: {ex.Message}");
            }
        }        // GET: Chat/FloatingChat/{chatRoomId} - API endpoint to get messages for floating chat
        [HttpGet]
        [Route("FloatingChat/{id}")]
        [Route("Chat/FloatingChat/{id}")]
        public async Task<IActionResult> GetFloatingChatMessages(int id, int limit = 20)
        {
            var userId = GetCurrentUserId();
            
            // Check if user is authenticated
            if (userId == 0)
            {
                return Unauthorized("User not authenticated");
            }
            
            // Check if user can access this chat room
            var canAccess = await _chatService.CanUserAccessChatAsync(id, userId);
            if (!canAccess)
            {
                return Forbid();
            }

            try
            {
                var messages = await _chatService.GetChatMessagesAsync(id, limit);
                var messageViewModels = messages.Select(m => new ChatMessageViewModel
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
                    SenderRole = GetUserRole(m.SenderId ?? 0, m.ChatRoom)
                }).ToList();

                return Json(messageViewModels);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Failed to load messages", details = ex.Message });
            }
        }

        // Fix for CS1061: Replace 'AdminId' with 'AdminUserId' as per the ChatRoom type definition.
        private string GetUserRole(int senderId, ChatRoom chatRoom)
        {
            if (chatRoom?.Task?.WorkerId == senderId)
                return "Worker";
            if (chatRoom?.Task?.CustomerId == senderId)
                return "Customer";
            if (chatRoom?.AdminUserId == senderId) // Corrected property name
                return "Admin";
            return "Unknown";
        }

        // GET: Chat/GetChatRooms - Get all chat rooms for the current user        [HttpGet]
        [Route("GetChatRooms")]
        [Route("Chat/GetChatRooms")]
        public async Task<IActionResult> GetChatRooms()
        {
            var userId = GetCurrentUserId();
            
            // Check if user is authenticated
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
                });
                
                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving chat rooms: {ex.Message}");
            }
        }

        // GET: Chat/Test/{chatRoomId} - Test endpoint for debugging
        [HttpGet]
        [Route("Test/{chatRoomId}")]
        [Route("Chat/Test/{chatRoomId}")]
        public async Task<IActionResult> TestChatRoom(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            
            Console.WriteLine($"[TEST] Testing chat room {chatRoomId} for user {userId}");
            
            try 
            {
                // Test 1: Check if user can access
                var canAccess = await _chatService.CanUserAccessChatAsync(chatRoomId, userId);
                Console.WriteLine($"[TEST] Can access: {canAccess}");
                
                if (!canAccess) 
                {
                    return Json(new { 
                        success = false, 
                        error = "User cannot access this chat room",
                        userId = userId,
                        chatRoomId = chatRoomId
                    });
                }
                
                // Test 2: Get messages
                var messages = await _chatService.GetChatMessagesAsync(chatRoomId, 1, 10);
                Console.WriteLine($"[TEST] Messages count: {messages.Count()}");
                
                // Test 3: Get chat room info
                var chatRoom = await _chatService.GetChatRoomByIdAsync(chatRoomId);
                Console.WriteLine($"[TEST] Chat room found: {chatRoom != null}");
                
                return Json(new { 
                    success = true,
                    canAccess = canAccess,
                    messagesCount = messages.Count(),
                    chatRoomExists = chatRoom != null,
                    userId = userId,
                    chatRoomId = chatRoomId,
                    messages = messages.Take(3).Select(m => new {
                        id = m.Id,
                        content = m.Content,
                        senderName = m.Sender?.FullName ?? "Unknown",
                        sentAt = m.SentAt
                    })
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST] Error: {ex.Message}");
                return Json(new { 
                    success = false, 
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // GET: Chat/Test - Test page for debugging
        [HttpGet]
        [Route("Test")]
        [Route("Chat/Test")]
        public IActionResult Test()
        {
            return View();
        }
    }
}
