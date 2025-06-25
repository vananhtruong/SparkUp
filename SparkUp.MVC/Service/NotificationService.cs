using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SparkUp.Business;
using SparkUp.MVC.Hubs;

namespace SparkUp.MVC.Service
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> notificationHub)
        {
            _context = context;
            _notificationHub = notificationHub;
        }

        /// <inheritdoc />
        public async Task<Notification> CreateNotificationAsync(int userId, string title, string content, string type,
            string referenceId = null, string action = null, string redirectUrl = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Content = content,
                Type = type,
                ReferenceId = referenceId,
                Action = action,
                RedirectUrl = redirectUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        /// <inheritdoc />
        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int limit = 20, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task DeleteNotificationAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc />
        public async Task<int> CountUnreadNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task CreateBookingNotificationAsync(SparkUp.Business.Task taskBooking, string action)
        {
            // Tạo thông báo dựa trên action
            switch (action.ToLower())
            {
                case "create":
                    // Thông báo cho thợ khi có đặt lịch mới
                    var worker = await _context.Users.FindAsync(taskBooking.WorkerId);
                    var customer = await _context.Users.FindAsync(taskBooking.CustomerId);

                    if (worker != null && customer != null)
                    {
                        // Thông báo cho thợ
                        await CreateNotificationAsync(
                            userId: taskBooking.WorkerId,
                            title: "Yêu cầu đặt lịch mới",
                            content: $"Bạn có yêu cầu đặt lịch mới từ {customer.FullName} vào lúc {taskBooking.ScheduledTime:dd/MM/yyyy HH:mm}",
                            type: "Booking",
                            referenceId: taskBooking.Id.ToString(),
                            action: "view",
                            redirectUrl: $"/TaskBooking/Details/{taskBooking.Id}"
                        );
                    }
                    break;

                case "accept":
                    // Thông báo cho khách hàng khi thợ chấp nhận
                    await CreateNotificationAsync(
                        userId: taskBooking.CustomerId,
                        title: "Đặt lịch đã được chấp nhận",
                        content: $"Đặt lịch của bạn vào lúc {taskBooking.ScheduledTime:dd/MM/yyyy HH:mm} đã được chấp nhận",
                        type: "Booking",
                        referenceId: taskBooking.Id.ToString(),
                        action: "view",
                        redirectUrl: $"/TaskBooking/Details/{taskBooking.Id}"
                    );
                    break;

                case "reject":
                    // Thông báo cho khách hàng khi thợ từ chối
                    await CreateNotificationAsync(
                        userId: taskBooking.CustomerId,
                        title: "Đặt lịch đã bị từ chối",
                        content: $"Đặt lịch của bạn vào lúc {taskBooking.ScheduledTime:dd/MM/yyyy HH:mm} đã bị từ chối",
                        type: "Booking",
                        referenceId: taskBooking.Id.ToString(),
                        action: "view",
                        redirectUrl: $"/TaskBooking/Details/{taskBooking.Id}"
                    );
                    break;

                case "complete":
                    // Thông báo cho khách hàng khi công việc hoàn thành
                    await CreateNotificationAsync(
                        userId: taskBooking.CustomerId,
                        title: "Công việc đã hoàn thành",
                        content: $"Công việc của bạn đã được thợ đánh dấu hoàn thành. Vui lòng xác nhận và thanh toán.",
                        type: "Booking",
                        referenceId: taskBooking.Id.ToString(),
                        action: "payment",
                        redirectUrl: $"/Payment/Create/{taskBooking.Id}"
                    );
                    break;
            }
        }

        // ===== REAL-TIME NOTIFICATION METHODS =====

        /// <inheritdoc />
        public async Task<Notification> CreateAndSendNotificationAsync(int userId, string title, string content, string type,
            string referenceId = null, string action = null, string redirectUrl = null)
        {
            // Create notification in database
            var notification = await CreateNotificationAsync(userId, title, content, type, referenceId, action, redirectUrl);
            
            try
            {
                // Send real-time notification via SignalR
                await _notificationHub.Clients.Group($"User_{userId}")
                    .SendAsync("NewNotification", new
                    {
                        id = notification.Id,
                        title = notification.Title,
                        content = notification.Content,
                        type = notification.Type,
                        action = notification.Action,
                        redirectUrl = notification.RedirectUrl,
                        createdAt = notification.CreatedAt,
                        isRead = notification.IsRead
                    });

                // Update notification count
                await BroadcastNotificationCountAsync(userId);
                
                Console.WriteLine($"[NotificationService] Sent real-time notification to User_{userId}: {title}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationService] Failed to send real-time notification: {ex.Message}");
                // Don't throw - notification is still saved in DB
            }

            return notification;
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task SendChatNotificationAsync(int recipientUserId, int senderUserId, 
            string senderName, int chatRoomId, string messagePreview)
        {
            // Don't send notification to yourself
            if (recipientUserId == senderUserId) return;

            var title = $"Tin nhắn mới từ {senderName}";
            var content = messagePreview.Length > 100 ? 
                messagePreview.Substring(0, 100) + "..." : messagePreview;

            await CreateAndSendNotificationAsync(
                userId: recipientUserId,
                title: title,
                content: content,
                type: "Chat",
                referenceId: chatRoomId.ToString(),
                action: "view",
                redirectUrl: $"/Chat/Room/{chatRoomId}"
            );
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task SendBookingStatusNotificationAsync(int userId, int taskId, 
            string oldStatus, string newStatus, string taskTitle)
        {
            var title = "Trạng thái đặt lịch đã thay đổi";
            var content = $"Đặt lịch '{taskTitle}' đã chuyển từ {GetStatusDisplayName(oldStatus)} sang {GetStatusDisplayName(newStatus)}";

            await CreateAndSendNotificationAsync(
                userId: userId,
                title: title,
                content: content,
                type: "BookingStatus",
                referenceId: taskId.ToString(),
                action: "view",
                redirectUrl: $"/TaskBooking/Details/{taskId}"
            );
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task SendPaymentNotificationAsync(int userId, int taskId, 
            decimal amount, string paymentStatus)
        {
            var title = paymentStatus == "Success" ? "Thanh toán thành công" : "Thanh toán thất bại";
            var content = paymentStatus == "Success" 
                ? $"Bạn đã thanh toán thành công {amount:N0} VNĐ"
                : $"Thanh toán {amount:N0} VNĐ không thành công. Vui lòng thử lại.";

            await CreateAndSendNotificationAsync(
                userId: userId,
                title: title,
                content: content,
                type: "Payment",
                referenceId: taskId.ToString(),
                action: paymentStatus == "Success" ? "view" : "retry",
                redirectUrl: paymentStatus == "Success" 
                    ? $"/TaskBooking/Details/{taskId}" 
                    : $"/Payment/Create/{taskId}"
            );
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task BroadcastNotificationCountAsync(int userId)
        {
            try
            {
                var unreadCount = await CountUnreadNotificationsAsync(userId);
                
                await _notificationHub.Clients.Group($"User_{userId}")
                    .SendAsync("NotificationCount", unreadCount);
                    
                Console.WriteLine($"[NotificationService] Broadcasted notification count {unreadCount} to User_{userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationService] Failed to broadcast notification count: {ex.Message}");
            }
        }

        // Helper method to get user-friendly status names
        private string GetStatusDisplayName(string status)
        {
            return status switch
            {
                "Pending" => "Chờ xác nhận",
                "Confirmed" => "Đã xác nhận", 
                "InProgress" => "Đang thực hiện",
                "Done" => "Hoàn thành",
                "Canceled" => "Đã hủy",
                _ => status
            };
        }
    }
}