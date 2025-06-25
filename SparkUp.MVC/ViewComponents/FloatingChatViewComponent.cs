using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;
using SparkUp.MVC.Models;
using System.Security.Claims;

namespace SparkUp.MVC.ViewComponents
{
    public class FloatingChatViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public FloatingChatViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int? taskBookingId = null)
        {
            var model = new FloatingChatViewModel
            {
                CanShowChat = false
            };

            // Kiểm tra user đã đăng nhập chưa
            if (!UserClaimsPrincipal.Identity.IsAuthenticated)
            {
                return View(model);
            }

            var userIdClaim = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return View(model);
            }

            // Nếu có taskBookingId, hiển thị chat cho booking đó
            if (taskBookingId.HasValue)
            {
                var booking = await _context.Tasks
                    .Include(t => t.Worker)
                    .Include(t => t.Customer)
                    .Include(t => t.TaskType)
                    .Include(t => t.ChatRoom)
                    .FirstOrDefaultAsync(t => t.Id == taskBookingId.Value);

                if (booking != null && 
                    (booking.WorkerId == userId || booking.CustomerId == userId) &&
                    (booking.Status == "Accepted" || booking.Status == "InProgress" || booking.Status == "Completed"))
                {
                    // Đếm số tin nhắn chưa đọc
                    int unreadCount = 0;
                    if (booking.ChatRoom != null)
                    {
                        unreadCount = await _context.ChatMessages
                            .Where(m => m.ChatRoomId == booking.ChatRoom.Id && 
                                       m.SenderId != userId && 
                                       !m.IsRead)
                            .CountAsync();
                    }

                    model = new FloatingChatViewModel
                    {
                        CanShowChat = true,
                        ChatRoomId = booking.ChatRoom?.Id,
                        TaskTitle = $"{booking.TaskType.Name} - #{booking.Id}",
                        CustomerName = booking.Customer.FullName,
                        WorkerName = booking.Worker.FullName,
                        IsWorker = booking.WorkerId == userId,
                        UnreadCount = unreadCount,
                        CurrentUserId = userId,
                        Status = booking.ChatRoom?.Status.ToString() ?? "NotCreated"
                    };
                }
            }
            else
            {
                // Hiển thị chat room gần đây nhất của user
                var recentChatRoom = await _context.ChatRooms
                    .Include(c => c.Task)
                        .ThenInclude(t => t.Worker)
                    .Include(c => c.Task)
                        .ThenInclude(t => t.Customer)
                    .Include(c => c.Task)
                        .ThenInclude(t => t.TaskType)
                    .Where(c => c.Task.WorkerId == userId || c.Task.CustomerId == userId)
                    .Where(c => c.Status == ChatRoomStatus.Active)
                    .OrderByDescending(c => c.LastMessageAt)
                    .FirstOrDefaultAsync();

                if (recentChatRoom != null)
                {
                    // Đếm số tin nhắn chưa đọc
                    int unreadCount = await _context.ChatMessages
                        .Where(m => m.ChatRoomId == recentChatRoom.Id && 
                                   m.SenderId != userId && 
                                   !m.IsRead)
                        .CountAsync();

                    model = new FloatingChatViewModel
                    {
                        CanShowChat = true,
                        ChatRoomId = recentChatRoom.Id,
                        TaskTitle = $"{recentChatRoom.Task.TaskType.Name} - #{recentChatRoom.Task.Id}",
                        CustomerName = recentChatRoom.Task.Customer.FullName,
                        WorkerName = recentChatRoom.Task.Worker.FullName,
                        IsWorker = recentChatRoom.Task.WorkerId == userId,
                        UnreadCount = unreadCount,
                        CurrentUserId = userId,
                        Status = recentChatRoom.Status.ToString()
                    };
                }
            }

            return View(model);
        }
    }
}
