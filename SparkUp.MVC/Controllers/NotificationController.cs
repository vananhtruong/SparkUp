using Microsoft.AspNetCore.Mvc;
using SparkUp.MVC.Service;
using System.Security.Claims;

namespace SparkUp.MVC.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // Lấy số lượng thông báo chưa đọc
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { count = 0 });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                var count = await _notificationService.CountUnreadNotificationsAsync(userId);
                return Json(new { count });
            }

            return Json(new { count = 0 });
        }

        // Lấy danh sách thông báo của người dùng
        [HttpGet]
        public async Task<IActionResult> GetUserNotifications(int limit = 10)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { notifications = new List<object>() });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                var notifications = await _notificationService.GetUserNotificationsAsync(userId, limit);
                return Json(new { notifications });
            }

            return Json(new { notifications = new List<object>() });
        }

        // Đánh dấu thông báo đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return BadRequest();
            }

            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }

        // Đánh dấu tất cả thông báo đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return BadRequest();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                await _notificationService.MarkAllAsReadAsync(userId);
                return Ok();
            }

            return BadRequest();
        }

        // Xóa thông báo
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return BadRequest();
            }

            await _notificationService.DeleteNotificationAsync(id);
            return Ok();
        }
    }
}
