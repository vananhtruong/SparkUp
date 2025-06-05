using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;
using SparkUp.MVC.Models;

namespace SparkUp.MVC.Controllers
{
    public class BookingManagementController : Controller
    {
        private readonly AppDbContext _context;

        public BookingManagementController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /BookingManagement/Index
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Authentication");
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra vai trò người dùng
            var isWorker = User.IsInRole("Worker");
            var role = isWorker ? "Worker" : "Customer";

            // Nếu là thợ, hiển thị BookingManagementViewModel với các đặt lịch của thợ
            // Nếu là khách hàng, hiển thị BookingManagementViewModel với các đặt lịch của khách hàng
            var bookingManagementViewModel = new BookingManagementViewModel
            {
                IsWorker = isWorker,
                UpcomingBookings = await GetBookings(userId, role, "upcoming"),
                PendingBookings = await GetBookings(userId, role, "pending"),
                CompletedBookings = await GetBookings(userId, role, "completed")
            };

            return View(bookingManagementViewModel);
        }

        private async Task<List<TaskBookingListViewModel>> GetBookings(int userId, string role, string filter)
        {
            var query = _context.Tasks.AsQueryable();

            // Lọc theo vai trò
            if (role == "Worker")
            {
                query = query.Where(t => t.WorkerId == userId);
            }
            else
            {
                query = query.Where(t => t.CustomerId == userId);
            }

            // Lọc theo trạng thái
            switch (filter.ToLower())
            {
                case "upcoming":
                    query = query.Where(t => t.Status == "Accepted" && t.ScheduledTime > DateTime.Now);
                    break;
                case "pending":
                    query = query.Where(t => t.Status == "Pending");
                    break;
                case "completed":
                    query = query.Where(t => t.Status == "Completed");
                    break;
                default:
                    break;
            }

            // Bao gồm các bảng liên quan
            query = query
                .Include(t => t.Worker)
                .Include(t => t.Customer)
                .Include(t => t.TaskType);

            // Sắp xếp theo thời gian
            query = query.OrderByDescending(t => t.ScheduledTime);

            // Ánh xạ sang ViewModel
            var bookings = await query.Select(t => new TaskBookingListViewModel
            {
                Id = t.Id,
                WorkerName = t.Worker.FullName,
                WorkerAvatar = t.Worker.AvatarUrl,
                CustomerName = t.Customer.FullName,
                CustomerAvatar = t.Customer.AvatarUrl,
                TaskTypeName = t.TaskType.Name,
                ScheduledTime = t.ScheduledTime,
                Address = t.Address,
                Description = t.Description,
                Status = t.Status,
                PaymentStatus = t.PaymentStatus,
                EstimatedWork = t.EstimatedWork,
                CreatedAt = t.CreatedAt
            }).ToListAsync();

            return bookings;
        }
    }
}