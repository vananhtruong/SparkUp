using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;

namespace SparkUp.MVC.Controllers.Admin
{
    [Authorize(Roles = "ADMIN")]
    public class TaskTypeController : Controller
    {
        private readonly AppDbContext _context;

        public TaskTypeController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var taskTypes = await _context.TaskTypes.ToListAsync();
            return View(taskTypes);
        }
        public IActionResult Create()
        {
            return View();
        }

        // POST: TaskType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskType taskType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(taskType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(taskType);
        }

        // GET: TaskType/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var taskType = await _context.TaskTypes.FindAsync(id);
            if (taskType == null)
                return NotFound();

            return View(taskType);
        }

        // POST: TaskType/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskType taskType)
        {
            if (id != taskType.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(taskType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(taskType);
        }

        // POST: TaskType/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var taskType = await _context.TaskTypes.FindAsync(id);
            if (taskType != null)
            {
                _context.TaskTypes.Remove(taskType);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
