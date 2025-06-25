using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;

namespace SparkUp.MVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataCheckController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DataCheckController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new { u.Id, u.FullName, u.Email, u.Role })
                .ToListAsync();

            return Ok(new { count = users.Count, users });
        }

        [HttpGet("tasks")]
        public async Task<IActionResult> GetTasks()
        {
            var tasks = await _context.Tasks
                .Include(t => t.TaskType)
                .Select(t => new { 
                    t.Id, 
                    t.Description, 
                    TaskType = t.TaskType.Name,
                    CustomerName = t.Customer.FullName,
                    WorkerName = t.Worker.FullName,
                    t.Status,
                    t.ScheduledTime
                })
                .ToListAsync();

            return Ok(new { count = tasks.Count, tasks });
        }

        [HttpGet("workers")]
        public async Task<IActionResult> GetWorkers()
        {
            var workers = await _context.WorkerProfiles
                .Include(w => w.User)
                .Include(w => w.TaskType)
                .Select(w => new { 
                    w.Id, 
                    WorkerName = w.User.FullName,
                    TaskType = w.TaskType.Name,
                    w.Skills,
                    w.City,
                    w.District,
                    w.RatingAverage,
                    w.HourlyRate
                })
                .ToListAsync();

            return Ok(new { count = workers.Count, workers });
        }
    }
}
