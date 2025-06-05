using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;
using SparkUp.MVC.Models;

namespace SparkUp.MVC.Controllers
{
    public class WorkerController : Controller
    {
        private readonly AppDbContext _context;

        public WorkerController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTaskTypesByName(string term)
        {
            var taskTypes = await _context.TaskTypes
                .Where(tt => tt.IsActive && tt.Name.Contains(term))
                .Select(tt => new { id = tt.Id, name = tt.Name })
                .Take(10)
                .ToListAsync();

            return Json(taskTypes);
        }
        public async Task<IActionResult> Index()
        {
            // Lấy các loại công việc phổ biến với icon tương ứng và giá tối thiểu
            var popularTaskTypes = await _context.TaskTypes
                .Where(tt => tt.IsActive)
                .OrderBy(tt => tt.Name)
                .Select(tt => new
                {
                    Id = tt.Id,
                    Name = tt.Name,
                    Icon = tt.Icon ?? GetIconForTaskType(tt.Name), // Sử dụng Icon từ DB hoặc helper
                    MinPrice = _context.WorkerProfiles
                        .Where(wp => wp.TaskTypeId == tt.Id)
                        .Min(wp => wp.HourlyRate)
                })
                .Take(8)
                .ToListAsync();

            ViewBag.PopularTaskTypes = popularTaskTypes;
            return View();
        }

        private string GetIconForTaskType(string taskType)
        {
            return taskType.ToLower() switch
            {
                var t when t.Contains("sửa chữa") => "bi-tools",
                var t when t.Contains("điện") => "bi-lightning-charge",
                var t when t.Contains("nước") => "bi-droplet",
                var t when t.Contains("chuyển") => "bi-truck",
                var t when t.Contains("dọn") => "bi-house-gear",
                var t when t.Contains("làm vườn") => "bi-flower1",
                var t when t.Contains("lắp đặt") => "bi-screwdriver",
                var t when t.Contains("sơn") => "bi-palette",
                _ => "bi-hammer" // Default icon
            };
        }
        // Trang tìm kiếm thợ
        public async Task<IActionResult> Search(WorkerSearchViewModel model)
        {
            // Lấy danh sách các loại công việc cho dropdown
            ViewBag.TaskTypes = await _context.TaskTypes
                .Where(tt => tt.IsActive)
                .Select(tt => new SelectListItem
                {
                    Value = tt.Id.ToString(),
                    Text = tt.Name
                })
                .ToListAsync();

            // Xây dựng query tìm kiếm thợ
            var workersQuery = _context.Users
                .Where(u => u.Role == "Worker" && u.IsActive)
                .Include(u => u.WorkerProfile)
                    .ThenInclude(wp => wp.TaskType)
                .Where(u => u.WorkerProfile != null && u.WorkerProfile.IsConfirmed)
                .AsQueryable();


            // Lọc theo địa điểm
            if (!string.IsNullOrEmpty(model.Location))
            {
                workersQuery = workersQuery.Where(u =>
                    (u.WorkerProfile.City != null && u.WorkerProfile.City.Contains(model.Location)) ||
                    (u.WorkerProfile.District != null && u.WorkerProfile.District.Contains(model.Location)) ||
                    (u.WorkerProfile.Address != null && u.WorkerProfile.Address.Contains(model.Location))
                );
            }

            // Lọc theo loại dịch vụ
            if (model.TaskTypeId.HasValue && model.TaskTypeId > 0)
            {
                workersQuery = workersQuery.Where(u => u.WorkerProfile.TaskTypeId == model.TaskTypeId);

                // Lưu tên loại dịch vụ đã chọn
                var selectedTaskType = await _context.TaskTypes
                    .Where(tt => tt.Id == model.TaskTypeId)
                    .Select(tt => tt.Name)
                    .FirstOrDefaultAsync();
                model.SelectedTaskType = selectedTaskType ?? "";
            }

            // Lọc theo giá tối đa
            if (model.MaxPrice.HasValue)
            {
                workersQuery = workersQuery.Where(u => u.WorkerProfile.HourlyRate <= model.MaxPrice);
            }

            // Sắp xếp kết quả
            workersQuery = model.SortBy switch
            {
                "PriceLowToHigh" => workersQuery.OrderBy(u => u.WorkerProfile.HourlyRate),
                "PriceHighToLow" => workersQuery.OrderByDescending(u => u.WorkerProfile.HourlyRate),
                _ => workersQuery.OrderByDescending(u => u.WorkerProfile.RatingAverage)
            };

            // Thực hiện truy vấn và chuyển đổi kết quả
            var workers = await workersQuery
                 .Select(u => new WorkerViewModel
                 {
                     Id = u.Id,
                     FullName = u.FullName,
                     AvatarUrl = u.AvatarUrl,
                     Rating = u.WorkerProfile.RatingAverage,
                     ExperienceYears = u.WorkerProfile.ExperienceYears,
                     Skills = u.WorkerProfile.Skills,
                     City = u.WorkerProfile.City,
                     District = u.WorkerProfile.District,
                     HourlyRate = u.WorkerProfile.HourlyRate,
                     TaskTypes = new List<string> { u.WorkerProfile.TaskType.Name }
                 })
                 .ToListAsync();

            model.Workers = workers;
            return View(model);
        }

        // Xem chi tiết thông tin thợ
        public async Task<IActionResult> Profile(int id)
        {
            var worker = await _context.Users
                 .Where(u => u.Id == id && u.Role == "Worker" && u.IsActive)
                 .Include(u => u.WorkerProfile)
                     .ThenInclude(wp => wp.TaskType)
                 .FirstOrDefaultAsync();

            if (worker == null)
            {
                return NotFound();
            }

            // Lấy các feedback của thợ này
            var feedbacks = await _context.Feedbacks
                .Where(f => f.ToWorker == id)
                .Include(f => f.FromUser)
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToListAsync();            var workerDetail = new WorkerDetailViewModel
            {
                Id = worker.Id,
                FullName = worker.FullName,
                AvatarUrl = worker.AvatarUrl,
                PhoneNumber = worker.PhoneNumber,
                Email = worker.Email,
                Rating = worker.WorkerProfile.RatingAverage,
                ExperienceYears = worker.WorkerProfile.ExperienceYears,
                Skills = worker.WorkerProfile.Skills,
                Description = worker.WorkerProfile.Description,
                City = worker.WorkerProfile.City,
                District = worker.WorkerProfile.District,
                Address = worker.WorkerProfile.Address,
                HourlyRate = worker.WorkerProfile.HourlyRate,
                TaskTypeId = worker.WorkerProfile.TaskTypeId, // Thêm TaskTypeId
                TaskTypes = new List<string> { worker.WorkerProfile.TaskType.Name },
                Feedbacks = feedbacks.Select(f => new FeedbackViewModel
                {
                    FromUserName = f.FromUser.FullName,
                    FromUserAvatar = f.FromUser.AvatarUrl,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt
                }).ToList()
            };            return View(workerDetail);
        }

        // Trang bảng công việc cho thợ
        public async Task<IActionResult> Dashboard()
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

            // Kiểm tra xem người dùng có phải là thợ không
            var isWorker = User.IsInRole("Worker");
            if (!isWorker)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy thông tin đặt lịch gần đây của thợ
            var recentBookings = await _context.Tasks
                .Where(t => t.WorkerId == userId)
                .Include(t => t.Customer)
                .Include(t => t.TaskType)
                .OrderByDescending(t => t.ScheduledTime)
                .Take(5)
                .Select(t => new TaskBookingListViewModel
                {
                    Id = t.Id,
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
                })
                .ToListAsync();

            // Thêm các thông tin khác cần thiết cho bảng điều khiển
            ViewBag.RecentBookings = recentBookings;
            ViewBag.PendingBookingsCount = await _context.Tasks
                .Where(t => t.WorkerId == userId && t.Status == "Pending")
                .CountAsync();
            ViewBag.CompletedBookingsCount = await _context.Tasks
                .Where(t => t.WorkerId == userId && t.Status == "Completed")
                .CountAsync();

            return View();
        }
    }
}