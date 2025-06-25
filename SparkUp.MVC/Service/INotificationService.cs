using System.Collections.Generic;
using System.Threading.Tasks;
using SparkUp.Business;

namespace SparkUp.MVC.Service
{
    public interface INotificationService
    {
        /// <summary>
        /// Tạo thông báo mới và lưu vào cơ sở dữ liệu
        /// </summary>
        Task<Notification> CreateNotificationAsync(int userId, string title, string content, string type,
            string referenceId = null, string action = null, string redirectUrl = null);

        /// <summary>
        /// Lấy danh sách thông báo của người dùng đã xác định
        /// </summary>
        Task<List<Notification>> GetUserNotificationsAsync(int userId, int limit = 20, bool unreadOnly = false);

        /// <summary>
        /// Đánh dấu thông báo đã đọc
        /// </summary>
        System.Threading.Tasks.Task MarkAsReadAsync(int notificationId);

        /// <summary>
        /// Đánh dấu tất cả thông báo của người dùng đã đọc
        /// </summary>
        System.Threading.Tasks.Task MarkAllAsReadAsync(int userId);

        /// <summary>
        /// Xóa thông báo
        /// </summary>
        System.Threading.Tasks.Task DeleteNotificationAsync(int notificationId);

        /// <summary>
        /// Đếm số thông báo chưa đọc của người dùng
        /// </summary>
        Task<int> CountUnreadNotificationsAsync(int userId);

        /// <summary>
        /// Tạo thông báo đặt lịch thợ
        /// </summary>
        System.Threading.Tasks.Task CreateBookingNotificationAsync(SparkUp.Business.Task taskBooking, string action);

        /// <summary>
        /// Tạo và gửi thông báo real-time
        /// </summary>
        Task<Notification> CreateAndSendNotificationAsync(int userId, string title, string content, string type,
            string referenceId = null, string action = null, string redirectUrl = null);

        /// <summary>
        /// Gửi thông báo chat message mới
        /// </summary>
        System.Threading.Tasks.Task SendChatNotificationAsync(int recipientUserId, int senderUserId, 
            string senderName, int chatRoomId, string messagePreview);

        /// <summary>
        /// Gửi thông báo booking status thay đổi
        /// </summary>
        System.Threading.Tasks.Task SendBookingStatusNotificationAsync(int userId, int taskId, 
            string oldStatus, string newStatus, string taskTitle);

        /// <summary>
        /// Gửi thông báo payment thành công
        /// </summary>
        System.Threading.Tasks.Task SendPaymentNotificationAsync(int userId, int taskId, 
            decimal amount, string paymentStatus);

        /// <summary>
        /// Broadcast notification count update to user
        /// </summary>
        System.Threading.Tasks.Task BroadcastNotificationCountAsync(int userId);
    }
}