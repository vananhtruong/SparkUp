using Microsoft.AspNetCore.Mvc;
using SparkUp.MVC.Service;
using System.Security.Claims;

namespace SparkUp.MVC.ViewComponents
{
    public class NotificationMenuViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationMenuViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return View("Default", new List<SparkUp.Business.Notification>());
            }

            var userIdClaim = (User.Identity as ClaimsIdentity)?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return View("Default", new List<SparkUp.Business.Notification>());
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 10);
            return View("Default", notifications);
        }
    }
}
