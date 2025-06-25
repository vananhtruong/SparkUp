using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;

namespace SparkUp.MVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataSeedController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DataSeedController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("seed")]
        public IActionResult SeedData()
        {
            try
            {
                // Kiểm tra trạng thái database trước khi seed
                var initialCheck = new
                {
                    UserCount = _context.Users.Count(),
                    TaskTypeCount = _context.TaskTypes.Count()
                };

                // Thực hiện seed data
                DataSeeder.SeedData(_context);
                
                // Kiểm tra sau khi seed để xác minh dữ liệu đã được thêm
                var finalCheck = new
                {
                    UserCount = _context.Users.Count(),
                    TaskTypeCount = _context.TaskTypes.Count()
                };

                return Ok(new { 
                    success = true, 
                    message = "Dữ liệu đã được thêm thành công!", 
                    before = initialCheck,
                    after = finalCheck
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi thêm dữ liệu: {ex.Message}", details = ex.StackTrace });
            }
        }

        [HttpGet("check")]
        public IActionResult CheckData()
        {
            try
            {
                var result = new
                {
                    UserCount = _context.Users.Count(),
                    TaskTypeCount = _context.TaskTypes.Count(),
                    WorkerProfileCount = _context.WorkerProfiles.Count(),
                    TaskCount = _context.Tasks.Count(),
                    ChatRoomCount = _context.ChatRooms.Count(),
                    ChatMessageCount = _context.ChatMessages.Count(),
                    WalletCount = _context.Wallets.Count(),
                    EmailTemplateCount = _context.EmailTemplates.Count()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi kiểm tra dữ liệu: {ex.Message}" });
            }
        }
        
        [HttpGet("details")]
        public IActionResult GetDataDetails()
        {
            try
            {
                var users = _context.Users.ToList();
                var taskTypes = _context.TaskTypes.ToList();
                var workers = _context.WorkerProfiles.Include(w => w.User).ToList();
                var tasks = _context.Tasks.Include(t => t.TaskType).Include(t => t.Worker).ToList();
                var wallets = _context.Wallets.Include(w => w.User).ToList();
                
                var result = new
                {
                    Users = users.Select(u => new { u.Id, u.Email, u.FullName, u.PhoneNumber }),
                    TaskTypes = taskTypes.Select(tt => new { tt.Id, tt.Name, tt.Description }),
                    Workers = workers.Select(w => new { w.Id, UserName = $"{w.User?.FullName}", w.Skills, w.HourlyRate }),
                    Tasks = tasks.Select(t => new { t.Id, t.TaskType, t.Description, TaskTypeName = t.TaskType?.Name, UserName = $"{t.Worker?.FullName}" }),
                    Wallets = wallets.Select(w => new { w.Id, UserName = $"{w.User?.FullName}", w.Balance })
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy chi tiết dữ liệu: {ex.Message}" });
            }
        }

        [HttpGet("clear")]
        public IActionResult ClearData()
        {
            try
            {
                // Thứ tự xóa: trước tiên phải xóa các bảng có foreign key
                _context.ChatMessages.RemoveRange(_context.ChatMessages.ToList());
                _context.ChatRooms.RemoveRange(_context.ChatRooms.ToList());
                _context.Tasks.RemoveRange(_context.Tasks.ToList());
                _context.WorkerProfiles.RemoveRange(_context.WorkerProfiles.ToList());
                _context.Wallets.RemoveRange(_context.Wallets.ToList());
                _context.Notifications.RemoveRange(_context.Notifications.ToList());
                _context.Payments.RemoveRange(_context.Payments.ToList());
                _context.Feedbacks.RemoveRange(_context.Feedbacks.ToList());
                _context.Users.RemoveRange(_context.Users.ToList());
                _context.TaskTypes.RemoveRange(_context.TaskTypes.ToList());
                _context.EmailTemplates.RemoveRange(_context.EmailTemplates.ToList());
                
                // Lưu các thay đổi
                _context.SaveChanges();

                return Ok(new { success = true, message = "Đã xóa toàn bộ dữ liệu!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi xóa dữ liệu: {ex.Message}", details = ex.StackTrace });
            }
        }
    }
}
