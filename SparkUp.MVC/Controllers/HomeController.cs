using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;
using SparkUp.MVC.Models;
using System.Diagnostics;

namespace SparkUp.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy các loại công việc phổ biến (chỉ lấy dữ liệu từ DB)
                var taskTypes = await _context.TaskTypes
                    .Where(tt => tt.IsActive)
                    .OrderBy(tt => tt.Name)
                    .Take(8)
                    .ToListAsync();

                // Lấy giá tối thiểu cho từng loại công việc
                var minPrices = await _context.WorkerTaskTypes
                    .GroupBy(wtt => wtt.TaskTypeId)
                    .Select(g => new { TaskTypeId = g.Key, MinPrice = g.Min(wtt => wtt.HourlyRate) })
                    .ToListAsync();

                // Kết hợp dữ liệu và gán icon
                var popularTaskTypes = taskTypes.Select(tt => new
                {
                    Id = tt.Id,
                    Name = tt.Name,
                    Icon = GetIconForTaskType(tt.Name), // Gán icon ở đây, không dùng DbSeeder
                    MinPrice = minPrices.FirstOrDefault(mp => mp.TaskTypeId == tt.Id)?.MinPrice ?? 50000M
                }).ToList();

                ViewBag.PopularTaskTypes = popularTaskTypes;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách loại công việc phổ biến");
                // Dữ liệu giả lập
                ViewBag.PopularTaskTypes = new List<dynamic>
        {
            new { Id = 1, Name = "Sửa điện", Icon = "bi-lightning-charge", MinPrice = 50000M },
            new { Id = 2, Name = "Sửa nước", Icon = "bi-droplet", MinPrice = 60000M },
            new { Id = 3, Name = "Vệ sinh", Icon = "bi-house-gear", MinPrice = 45000M },
            new { Id = 4, Name = "Chuyển nhà", Icon = "bi-truck", MinPrice = 120000M },
        };
                return View();
            }
        }



        private string GetIconForTaskType(string taskType)
        {
            return taskType.ToLower() switch
            {
                var t when t.Contains("sửa chữa") => "bi-tools",
                var t when t.Contains("điện") => "bi-lightning-charge",
                var t when t.Contains("nước") => "bi-droplet",
                var t when t.Contains("chuyển") => "bi-truck",
                var t when t.Contains("dọn") || t.Contains("vệ sinh") => "bi-house-gear",
                var t when t.Contains("làm vườn") => "bi-flower1",
                var t when t.Contains("lắp đặt") => "bi-screwdriver",
                var t when t.Contains("sơn") => "bi-palette",
                var t when t.Contains("khóa") => "bi-key",
                var t when t.Contains("máy lạnh") || t.Contains("điều hòa") => "bi-thermometer-snow",
                var t when t.Contains("đồ gỗ") || t.Contains("nội thất") => "bi-hammer",
                _ => "bi-hammer" // Icon mặc định
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
