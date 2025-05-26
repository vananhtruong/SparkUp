using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;

namespace SparkUp.MVC.Controllers.Admin
{
    public class WorkerManagementController : Controller
    {
        private readonly AppDbContext _context;

        public WorkerManagementController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            // Danh sách tất cả thợ (role = 'Worker')
            var workers = await _context.Users
                .Where(u => u.Role == "Worker")
                .Include(u => u.WorkerProfile)
                .ToListAsync();

            return View(workers);
        }

        // GET: /WorkerManagement/Pending
        public async Task<IActionResult> Pending()
        {
            var pendingProfiles = await _context.WorkerProfiles
                .Include(p => p.User)
                .Where(p => p.ApprovalStatus == "Pending" || p.ApprovalStatus == "Rejected")
                .ToListAsync();

            return View("Pending", pendingProfiles);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var profile = await _context.WorkerProfiles.FindAsync(id);
            if (profile == null) return NotFound();

            profile.ApprovalStatus = "Approved";
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Pending));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var profile = await _context.WorkerProfiles.FindAsync(id);
            if (profile == null) return NotFound();

            profile.ApprovalStatus = "Rejected";
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Pending));
        }


    }
}
