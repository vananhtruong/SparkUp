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
    }
}