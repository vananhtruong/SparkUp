using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;

namespace SparkUp.MVC.Service
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
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
    }
}