using Microsoft.EntityFrameworkCore;
using SparkUp.Business;
using SparkUp.MVC.Models;

namespace SparkUp.MVC.Service
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;

        public ChatService(AppDbContext context)
        {
            _context = context;
        }

        #region Chat Room Management

        public async Task<ChatRoom?> GetOrCreateChatRoomAsync(int taskId)
        {
            var existingChatRoom = await _context.ChatRooms
                .Include(cr => cr.Task)
                .Include(cr => cr.Customer)
                .Include(cr => cr.Worker)
                .Include(cr => cr.AdminUser)
                .FirstOrDefaultAsync(cr => cr.TaskId == taskId);

            if (existingChatRoom != null)
                return existingChatRoom;

            return await CreateChatRoomAsync(taskId);
        }

        public async Task<ChatRoom?> CreateChatRoomAsync(int taskId)
        {
            // Get task to create chat room
            var task = await _context.Tasks
                .Include(t => t.Customer)
                .Include(t => t.Worker)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null || task.CustomerId == null || task.WorkerId == null)
                return null;

            // Allow chat room creation for Accepted, InProgress, or Completed status
            if (task.Status != "Accepted" && task.Status != "InProgress" && task.Status != "Completed")
                return null;

            var chatRoom = new ChatRoom
            {
                TaskId = taskId,
                CustomerId = task.CustomerId,
                WorkerId = task.WorkerId,
                Status = ChatRoomStatus.Active,
                CreatedAt = DateTime.Now,
                LastMessageAt = DateTime.Now
            };

            _context.ChatRooms.Add(chatRoom);
            await _context.SaveChangesAsync();

            return await GetChatRoomByIdAsync(chatRoom.Id);
        }

        public async Task<ChatRoom?> GetChatRoomByIdAsync(int chatRoomId)
        {
            return await _context.ChatRooms
                .Include(cr => cr.Task)
                .Include(cr => cr.Customer)
                .Include(cr => cr.Worker)
                .Include(cr => cr.AdminUser)
                .Include(cr => cr.Messages.Take(1).OrderByDescending(m => m.SentAt))
                .FirstOrDefaultAsync(cr => cr.Id == chatRoomId);
        }

        public async Task<ChatRoom?> GetChatRoomByTaskIdAsync(int taskId)
        {
            return await _context.ChatRooms
                .Include(cr => cr.Task)
                .Include(cr => cr.Customer)
                .Include(cr => cr.Worker)
                .Include(cr => cr.AdminUser)
                .FirstOrDefaultAsync(cr => cr.TaskId == taskId);
        }

        public async Task<bool> IsChatAvailableForTaskAsync(int taskId, int userId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return false;

            // Chat is available if user is customer or worker and task is active
            return (task.CustomerId == userId || task.WorkerId == userId) &&
                   (task.Status == "Accepted" || task.Status == "InProgress" || task.Status == "Completed");
        }

        public async Task<ChatRoom?> UpdateChatRoomStatusAsync(int chatRoomId, ChatRoomStatus status)
        {
            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom == null) return null;

            chatRoom.Status = status;
            if (status == ChatRoomStatus.Resolved)
                chatRoom.ResolvedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return await GetChatRoomByIdAsync(chatRoomId);
        }

        #endregion

        #region Message Management

        public async Task<ChatMessage> SendMessageAsync(int chatRoomId, int senderId, string content, ChatMessageType messageType = ChatMessageType.Text)
        {
            // Validate inputs
            if (chatRoomId <= 0)
            {
                throw new ArgumentException("Invalid chat room ID", nameof(chatRoomId));
            }

            if (senderId <= 0)
            {
                throw new ArgumentException("Invalid sender ID", nameof(senderId));
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Message content cannot be empty", nameof(content));
            }

            // Check if chat room exists
            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom == null)
            {
                throw new InvalidOperationException($"Chat room with ID {chatRoomId} not found");
            }

            // Create the message with validated data
            var message = new ChatMessage
            {
                ChatRoomId = chatRoomId,
                SenderId = senderId,
                Content = content.Trim(),
                SentAt = DateTime.Now,
                MessageType = messageType,
                IsRead = false,
                IsDeleted = false
            }; try
            {
                // First, check for any validation issues with the message
                if (message == null)
                {
                    throw new InvalidOperationException("Message object is null");
                }

                // Ensure ChatRoomId is valid
                if (message.ChatRoomId <= 0)
                {
                    throw new InvalidOperationException($"Invalid ChatRoomId: {message.ChatRoomId}");
                }

                // Ensure Content is not null
                if (message.Content == null)
                {
                    message.Content = string.Empty; // Provide a default to avoid null reference
                }

                // Add the message to the context
                _context.ChatMessages.Add(message);

                // Update chat room's last message time
                chatRoom.LastMessageAt = DateTime.Now;

                Console.WriteLine($"Saving message to database: ChatRoomId={chatRoomId}, SenderId={senderId}, MessageType={messageType}, Content={content.Length} chars");

                // Try to save with extra validation checks
                await _context.SaveChangesAsync();
                Console.WriteLine($"Successfully saved message with ID: {message.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error while saving message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Additional diagnostics
                try
                {
                    // Check if it's a validation-related exception
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                    }

                    // Print out the entity state for debugging
                    var entities = _context.ChangeTracker.Entries()
                        .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added ||
                                   e.State == Microsoft.EntityFrameworkCore.EntityState.Modified)
                        .ToList();

                    Console.WriteLine($"Number of tracked entities: {entities.Count}");

                    foreach (var entity in entities)
                    {
                        Console.WriteLine($"Entity type: {entity.Entity.GetType().Name}, State: {entity.State}");
                    }
                }
                catch (Exception validationEx)
                {
                    Console.WriteLine($"Error checking validation errors: {validationEx.Message}");
                }

                throw; // Re-throw the exception to be handled by the caller
            }

            return await _context.ChatMessages
                .Include(m => m.Sender)
                .FirstAsync(m => m.Id == message.Id);
        }

        public async Task<ChatMessage> SendSystemMessageAsync(int chatRoomId, string content)
        {
            var message = new ChatMessage
            {
                ChatRoomId = chatRoomId,
                SenderId = null, // System message has no sender
                Content = content.Trim(),
                SentAt = DateTime.Now,
                MessageType = ChatMessageType.System,
                IsRead = false,
                IsDeleted = false
            };

            _context.ChatMessages.Add(message);

            // Update chat room's last message time
            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom != null)
            {
                chatRoom.LastMessageAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return await _context.ChatMessages
                .Include(m => m.Sender)
                .FirstAsync(m => m.Id == message.Id);
        }

        public async Task<List<ChatMessage>> GetChatMessagesAsync(int chatRoomId, int page = 1, int pageSize = 50)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.ChatRoomId == chatRoomId && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<ChatMessage?> GetMessageByIdAsync(int messageId)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.ChatRoom)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        public async Task<bool> MarkMessageAsReadAsync(int messageId, int userId)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null || message.SenderId == userId) return false;

            message.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllMessagesAsReadAsync(int chatRoomId, int userId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.ChatRoomId == chatRoomId &&
                           m.SenderId != userId &&
                           !m.IsRead &&
                           !m.IsDeleted)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadMessageCountAsync(int chatRoomId, int userId)
        {
            return await _context.ChatMessages
                .CountAsync(m => m.ChatRoomId == chatRoomId &&
                               m.SenderId != userId &&
                               !m.IsRead &&
                               !m.IsDeleted);
        }

        #endregion

        #region Dispute Management

        public async Task<ChatRoom?> InitiateDisputeAsync(int chatRoomId, int initiatorId, string reason)
        {
            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom == null || chatRoom.Status == ChatRoomStatus.Disputed)
                return null;

            chatRoom.Status = ChatRoomStatus.Disputed;
            chatRoom.DisputeReason = reason;
            chatRoom.DisputedAt = DateTime.Now;

            // Send system message about dispute
            await SendMessageAsync(chatRoomId, initiatorId,
                $"Dispute initiated: {reason}", ChatMessageType.DisputeInitiated);

            await _context.SaveChangesAsync();
            return await GetChatRoomByIdAsync(chatRoomId);
        }

        public async Task<ChatRoom?> AssignAdminToChatAsync(int chatRoomId, int adminId)
        {
            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom == null) return null;

            chatRoom.AdminUserId = adminId;
            chatRoom.Status = ChatRoomStatus.AdminReviewing;

            // Send system message about admin joining
            await SendMessageAsync(chatRoomId, adminId,
                "An administrator has joined the chat to help resolve this dispute.",
                ChatMessageType.System);

            await _context.SaveChangesAsync();
            return await GetChatRoomByIdAsync(chatRoomId);
        }

        public async Task<DisputeResolution> ResolveDisputeAsync(int chatRoomId, int adminId, string resolution, string actionTaken, decimal? refundAmount = null, string? notes = null)
        {
            var disputeResolution = new DisputeResolution
            {
                ChatRoomId = chatRoomId,
                AdminUserId = adminId,
                Resolution = resolution,
                ActionTaken = actionTaken,
                RefundAmount = refundAmount,
                Notes = notes,
                CreatedAt = DateTime.Now
            };

            _context.DisputeResolutions.Add(disputeResolution);

            // Update chat room status
            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom != null)
            {
                chatRoom.Status = ChatRoomStatus.Resolved;
                chatRoom.ResolvedAt = DateTime.Now;
            }

            // Send system message about resolution
            await SendMessageAsync(chatRoomId, adminId,
                $"Dispute resolved: {resolution}", ChatMessageType.DisputeResolved);

            await _context.SaveChangesAsync();

            return await _context.DisputeResolutions
                .Include(dr => dr.AdminUser)
                .FirstAsync(dr => dr.Id == disputeResolution.Id);
        }

        public async Task<List<DisputeResolution>> GetDisputeHistoryAsync(int chatRoomId)
        {
            return await _context.DisputeResolutions
                .Include(dr => dr.AdminUser)
                .Where(dr => dr.ChatRoomId == chatRoomId)
                .OrderByDescending(dr => dr.CreatedAt)
                .ToListAsync();
        }

        #endregion

        #region Admin Management

        public async Task<List<ChatRoom>> GetPendingDisputesAsync()
        {
            return await _context.ChatRooms
                .Include(cr => cr.Task)
                .Include(cr => cr.Customer)
                .Include(cr => cr.Worker)
                .Where(cr => cr.Status == ChatRoomStatus.Disputed)
                .OrderBy(cr => cr.DisputedAt)
                .ToListAsync();
        }

        public async Task<List<ChatRoom>> GetChatRoomsForAdminAsync(int adminId)
        {
            return await _context.ChatRooms
                .Include(cr => cr.Task)
                .Include(cr => cr.Customer)
                .Include(cr => cr.Worker)
                .Where(cr => cr.AdminUserId == adminId)
                .OrderByDescending(cr => cr.LastMessageAt)
                .ToListAsync();
        }

        #endregion

        #region User Access Control

        public async Task<bool> CanUserAccessChatAsync(int chatRoomId, int userId)
        {
            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom == null) return false;

            return chatRoom.CustomerId == userId ||
                   chatRoom.WorkerId == userId ||
                   chatRoom.AdminUserId == userId;
        }

        public async Task<string> GetUserRoleInChatAsync(int chatRoomId, int userId)
        {
            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom == null) return "None";

            if (chatRoom.CustomerId == userId) return "Customer";
            if (chatRoom.WorkerId == userId) return "Worker";
            if (chatRoom.AdminUserId == userId) return "Admin";

            return "None";
        }

        #endregion

        #region Chat Statistics

        public async Task<int> GetActiveChatCountAsync()
        {
            return await _context.ChatRooms
                .CountAsync(cr => cr.Status == ChatRoomStatus.Active);
        }

        public async Task<int> GetDisputedChatCountAsync()
        {
            return await _context.ChatRooms
                .CountAsync(cr => cr.Status == ChatRoomStatus.Disputed ||
                                 cr.Status == ChatRoomStatus.AdminReviewing);
        }

        public async Task<ChatStatisticsViewModel> GetChatStatisticsAsync()
        {
            var totalChats = await _context.ChatRooms.CountAsync();
            var activeChats = await _context.ChatRooms.CountAsync(cr => cr.Status == ChatRoomStatus.Active);
            var disputedChats = await _context.ChatRooms.CountAsync(cr => cr.Status == ChatRoomStatus.Disputed);
            var pendingReview = await _context.ChatRooms.CountAsync(cr => cr.Status == ChatRoomStatus.AdminReviewing);
            var resolvedDisputes = await _context.ChatRooms.CountAsync(cr => cr.Status == ChatRoomStatus.Resolved);
            var closedChats = await _context.ChatRooms.CountAsync(cr => cr.Status == ChatRoomStatus.Closed);

            // Calculate average resolution time
            var resolvedDisputesWithTime = await _context.ChatRooms
                .Where(cr => cr.Status == ChatRoomStatus.Resolved &&
                           cr.DisputedAt != null &&
                           cr.ResolvedAt != null)
                .Select(cr => new {
                    DisputedAt = cr.DisputedAt!.Value,
                    ResolvedAt = cr.ResolvedAt!.Value
                })
                .ToListAsync();

            var avgResolutionHours = resolvedDisputesWithTime.Any()
                ? resolvedDisputesWithTime.Average(x => (x.ResolvedAt - x.DisputedAt).TotalHours)
                : 0;

            return new ChatStatisticsViewModel
            {
                TotalChatRooms = totalChats,
                ActiveChats = activeChats,
                DisputedChats = disputedChats,
                ResolvedDisputes = resolvedDisputes,
                ClosedChats = closedChats,
                PendingAdminReview = pendingReview,
                AverageResolutionTimeHours = Math.Round(avgResolutionHours, 2),
                LastUpdated = DateTime.Now
            };
        }

        #endregion

        #region User Management

        public async Task<List<ChatRoomViewModel>> GetUserChatRoomsAsync(int userId)
        {
            // Find chat rooms where the user is either customer, worker, or admin
            var chatRooms = await _context.ChatRooms
                .Include(cr => cr.Task)
                .Include(cr => cr.Customer)
                .Include(cr => cr.Worker)
                .Include(cr => cr.AdminUser)
                .Where(cr =>
                    cr.Task.CustomerId == userId ||
                    cr.Task.WorkerId == userId ||
                    cr.AdminUserId == userId)
                .OrderByDescending(cr => cr.LastMessageAt)
                .ToListAsync();

            // Convert to view models
            var chatRoomViewModels = new List<ChatRoomViewModel>();

            foreach (var chatRoom in chatRooms)
            {
                // Get the last message and unread count
                var lastMessage = await _context.ChatMessages
                    .Where(m => m.ChatRoomId == chatRoom.Id && !m.IsDeleted)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                // Get unread count
                var unreadCount = await _context.ChatMessages
                    .CountAsync(m => m.ChatRoomId == chatRoom.Id &&
                               m.SenderId != userId &&
                               !m.IsRead &&
                               !m.IsDeleted);

                // Create view model
                var viewModel = new ChatRoomViewModel
                {
                    Id = chatRoom.Id,
                    TaskId = chatRoom.TaskId,
                    TaskTitle = chatRoom.Task?.Description ?? $"Công việc #{chatRoom.TaskId}",
                    CustomerName = chatRoom.Customer?.FullName ?? "Unknown",
                    WorkerName = chatRoom.Worker?.FullName ?? "Unknown",
                    AdminName = chatRoom.AdminUser?.FullName,
                    Status = chatRoom.Status,
                    CreatedAt = chatRoom.CreatedAt,
                    DisputedAt = chatRoom.DisputedAt,
                    ResolvedAt = chatRoom.ResolvedAt,
                    LastMessageAt = lastMessage?.SentAt ?? chatRoom.LastMessageAt,
                    UnreadCount = unreadCount,
                    UserRole = userId == chatRoom.CustomerId ? "Customer" :
                               userId == chatRoom.WorkerId ? "Worker" :
                               userId == chatRoom.AdminUserId ? "Admin" : "None",
                    CanAccess = true
                };

                // Set last message content if available
                if (lastMessage != null)
                {
                    viewModel.LastMessage = lastMessage.MessageType == ChatMessageType.Text
                        ? (lastMessage.Content.Length > 30
                            ? lastMessage.Content.Substring(0, 27) + "..."
                            : lastMessage.Content)
                        : "[File]";
                }
                else
                {
                    viewModel.LastMessage = "No messages yet";
                }

                chatRoomViewModels.Add(viewModel);
            }

            return chatRoomViewModels;
        }    }
    #endregion
}