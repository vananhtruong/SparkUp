using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;
using SparkUp.MVC.Models;
using Task = SparkUp.Business.Task;

namespace SparkUp.MVC.Controllers
{
    public class TaskBookingController : Controller
    {
        private readonly AppDbContext _context;

        public TaskBookingController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TaskBooking/Create
        public async Task<IActionResult> Create(int workerId, int taskTypeId)
        {
            var worker = await _context.Users
                .Include(u => u.WorkerProfile)
                .FirstOrDefaultAsync(u => u.Id == workerId);

            if (worker == null) return NotFound();

            var viewModel = new TaskBookingViewModel
            {
                WorkerId = workerId,
                TaskTypeId = taskTypeId,
                WorkDate = DateTime.Today.AddDays(1),
                AvailableSlots = await GetAvailableTimeSlotsForDate(workerId, DateTime.Today.AddDays(1))
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TaskBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableSlots = await GetAvailableTimeSlotsForDate(model.WorkerId, model.WorkDate);
                return View(model);
            }

            // Kiểm tra xem thời gian có hợp lệ không
            if (!await IsTimeSlotAvailable(model.WorkerId, model.WorkDate, model.StartTime, model.EstimatedHours))
            {
                ModelState.AddModelError("", "Thời gian đã được đặt hoặc không hợp lệ");
                model.AvailableSlots = await GetAvailableTimeSlotsForDate(model.WorkerId, model.WorkDate);
                return View(model);
            }

            // Tạo booking mới
            var booking = new Task
            {
                CustomerId = 1, // TODO: Lấy từ user đăng nhập
                WorkerId = model.WorkerId,
                TaskTypeId = model.TaskTypeId,
                Address = model.Address,
                Description = model.Description,
                ScheduledTime = model.WorkDate.Add(model.StartTime),
                EstimatedWork = $"Ước tính {model.EstimatedHours} giờ",
                Status = "Pending",
                PaymentStatus = "Unpaid",
                CreatedAt = DateTime.Now
            };

            // Tạo lịch làm việc cho thợ
            var schedule = new WorkerSchedule
            {
                WorkerId = model.WorkerId,
                StartTime = model.WorkDate.Add(model.StartTime),
                EndTime = model.WorkDate.Add(model.StartTime).AddHours(model.EstimatedHours),
                Type = "Booked",
                TaskId = booking.Id,
                Note = $"Công việc: {booking.Description}"
            };

            _context.Tasks.Add(booking);
            _context.WorkerSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Task", new { id = booking.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(int workerId, DateTime date)
        {
            var slots = await GetAvailableTimeSlotsForDate(workerId, date);
            return Json(slots);
        }

        private async Task<List<WorkerScheduleSlot>> GetAvailableTimeSlotsForDate(int workerId, DateTime date)
        {
            // Lấy tất cả lịch đã đặt của thợ trong ngày
            var existingSchedules = await _context.WorkerSchedules
                .Where(w => w.WorkerId == workerId 
                    && w.StartTime.Date == date.Date)
                .OrderBy(w => w.StartTime)
                .ToListAsync();

            var slots = new List<WorkerScheduleSlot>();
            var startHour = 8; // Bắt đầu từ 8h sáng
            var endHour = 17;  // Kết thúc lúc 17h chiều

            for (int hour = startHour; hour < endHour; hour++)
            {
                var slotStart = new TimeSpan(hour, 0, 0);
                var slotEnd = new TimeSpan(hour + 1, 0, 0);
                
                var isAvailable = !existingSchedules.Any(s => 
                    (s.StartTime.TimeOfDay <= slotStart && s.EndTime.TimeOfDay > slotStart) ||
                    (s.StartTime.TimeOfDay < slotEnd && s.EndTime.TimeOfDay >= slotEnd));

                slots.Add(new WorkerScheduleSlot
                {
                    StartTime = slotStart,
                    EndTime = slotEnd,
                    IsAvailable = isAvailable
                });
            }

            return slots;
        }

        private async Task<bool> IsTimeSlotAvailable(int workerId, DateTime date, TimeSpan startTime, int hours)
        {
            var endTime = startTime.Add(TimeSpan.FromHours(hours));

            // Kiểm tra thời gian làm việc có nằm trong khung giờ cho phép
            if (startTime < new TimeSpan(8, 0, 0) || endTime > new TimeSpan(17, 0, 0))
                return false;

            // Kiểm tra xem có trùng với lịch khác không
            var hasConflict = await _context.WorkerSchedules
                .AnyAsync(w => w.WorkerId == workerId 
                    && w.StartTime.Date == date.Date
                    && ((w.StartTime.TimeOfDay <= startTime && w.EndTime.TimeOfDay > startTime)
                    || (w.StartTime.TimeOfDay < endTime && w.EndTime.TimeOfDay >= endTime)));

            return !hasConflict;
        }
    }
}