using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;
using SparkUp.MVC.Models;
using SparkUp.MVC.Service;
using Task = SparkUp.Business.Task;

namespace SparkUp.MVC.Controllers
{
    public class TaskBookingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public TaskBookingController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;        }

        // GET: TaskBooking/Create
        public async Task<IActionResult> Create(int workerId, int? taskTypeId = null)
        {
            // Kiểm tra danh sách loại công việc có tồn tại
            var taskTypesExist = await _context.TaskTypes.AnyAsync();
            if (!taskTypesExist)
            {
                TempData["ErrorMessage"] = "Chưa có loại công việc nào trong hệ thống";
                return RedirectToAction("Index", "Home");
            }
            
            // Kiểm tra thợ có tồn tại không
            var worker = await _context.Users
                .Include(u => u.WorkerProfile)
                .ThenInclude(wp => wp.TaskType)
                .FirstOrDefaultAsync(u => u.Id == workerId);

            if (worker == null) return NotFound("Thợ không tồn tại");
            if (worker.WorkerProfile == null) return NotFound("Thợ không có hồ sơ thợ");
              // Nếu không có taskTypeId, thử lấy loại công việc đầu tiên của thợ
            if (!taskTypeId.HasValue)
            {
                Console.WriteLine($"Không có taskTypeId được cung cấp cho WorkerId={workerId}");
                
                if (worker.WorkerProfile?.TaskType != null)
                {
                    taskTypeId = worker.WorkerProfile.TaskType.Id;
                    Console.WriteLine($"Sử dụng TaskTypeId={taskTypeId} từ hồ sơ thợ");
                }
                else
                {
                    // Hoặc lấy loại công việc đầu tiên trong hệ thống
                    var firstTaskType = await _context.TaskTypes.FirstOrDefaultAsync();
                    if (firstTaskType != null)
                    {
                        taskTypeId = firstTaskType.Id;
                        Console.WriteLine($"Sử dụng TaskTypeId={taskTypeId} từ loại công việc đầu tiên trong hệ thống");
                    }
                    else
                    {
                        Console.WriteLine("Không tìm thấy loại công việc nào trong hệ thống");
                        return NotFound("Không có loại công việc nào trong hệ thống");
                    }
                }
            }
            else
            {
                // Kiểm tra loại công việc có tồn tại không
                var taskType = await _context.TaskTypes.FindAsync(taskTypeId);
                if (taskType == null) return NotFound("Loại công việc không tồn tại");            }
            
            var viewModel = new TaskBookingViewModel
            {
                WorkerId = workerId,
                TaskTypeId = taskTypeId.Value, // Lúc này taskTypeId đã được đảm bảo có giá trị
                WorkDate = DateTime.Today.AddDays(1),
                AvailableSlots = await GetAvailableTimeSlotsForDate(workerId, DateTime.Today.AddDays(1))
            };

            return View(viewModel);
        }        [HttpPost]
        public async Task<IActionResult> Create(TaskBookingViewModel model)
        {
            // Log để theo dõi dữ liệu đầu vào
            Console.WriteLine($"Model received: WorkerId={model.WorkerId}, TaskTypeId={model.TaskTypeId}, WorkDate={model.WorkDate}, StartTime={model.StartTime}, EstimatedHours={model.EstimatedHours}");
            
            // Loại bỏ validation error cho AvailableSlots nếu có
            ModelState.Remove("AvailableSlots");
            
            // Kiểm tra nếu StartTime là TimeSpan default (00:00:00) thì có thể là lỗi binding
            if (model.StartTime == TimeSpan.Zero)
            {
                ModelState.AddModelError("StartTime", "Vui lòng chọn thời gian bắt đầu");
                model.AvailableSlots = await GetAvailableTimeSlotsForDate(model.WorkerId, model.WorkDate);
                return View(model);
            }
              // Kiểm tra TaskTypeId có tồn tại trong database không
            var taskTypeExists = await _context.TaskTypes.AnyAsync(t => t.Id == model.TaskTypeId);
            if (!taskTypeExists)
            {
                Console.WriteLine($"TaskTypeId {model.TaskTypeId} không tồn tại trong database");
                ModelState.AddModelError("TaskTypeId", "Loại công việc không tồn tại");
                model.AvailableSlots = await GetAvailableTimeSlotsForDate(model.WorkerId, model.WorkDate);
                return View(model);
            }
            
            // Kiểm tra WorkerId có tồn tại trong database không
            var workerExists = await _context.Users.AnyAsync(u => u.Id == model.WorkerId && u.WorkerProfile != null);
            if (!workerExists)
            {
                Console.WriteLine($"WorkerId {model.WorkerId} không tồn tại hoặc không phải là thợ");
                ModelState.AddModelError("WorkerId", "Thợ không tồn tại");
                model.AvailableSlots = await GetAvailableTimeSlotsForDate(model.WorkerId, model.WorkDate);
                return View(model);
            }
            
            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState)
                {
                    foreach (var error in modelState.Value.Errors)
                    {
                        Console.WriteLine($"Validation Error: {modelState.Key} - {error.ErrorMessage}");
                    }
                }
                
                model.AvailableSlots = await GetAvailableTimeSlotsForDate(model.WorkerId, model.WorkDate);
                return View(model);
            }

            // Kiểm tra xem thời gian có hợp lệ không
            if (!await IsTimeSlotAvailable(model.WorkerId, model.WorkDate, model.StartTime, model.EstimatedHours))
            {
                Console.WriteLine($"TimeSlot not available: {model.WorkDate:yyyy-MM-dd} at {model.StartTime} for {model.EstimatedHours} hours");
                ModelState.AddModelError("", "Thời gian đã được đặt hoặc không hợp lệ");
                model.AvailableSlots = await GetAvailableTimeSlotsForDate(model.WorkerId, model.WorkDate);
                return View(model);            
            }
            
            // Kiểm tra người dùng đã đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Authentication");
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }
            
            Task booking = null;
            
            try
            {
                // Tạo booking mới
                booking = new Task
                {
                    CustomerId = userId,
                    WorkerId = model.WorkerId,
                    TaskTypeId = model.TaskTypeId,
                    Address = model.Address,
                    Description = model.Description,
                    ScheduledTime = model.WorkDate.Add(model.StartTime),
                    EstimatedWork = $"Ước tính {model.EstimatedHours} giờ",
                    Status = "Pending",
                    PaymentStatus = "Unpaid",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                Console.WriteLine($"Creating booking: CustomerId={booking.CustomerId}, WorkerId={booking.WorkerId}, ScheduledTime={booking.ScheduledTime}");

                // Sử dụng transaction để đảm bảo tính nhất quán dữ liệu
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {                        Console.WriteLine($"Thông tin booking trước khi lưu: CustomerId={booking.CustomerId}, WorkerId={booking.WorkerId}, TaskTypeId={booking.TaskTypeId}");
                        
                        // Double-check foreign keys before saving
                        var taskTypeCheck = await _context.TaskTypes.FindAsync(booking.TaskTypeId);
                        if (taskTypeCheck == null)
                        {
                            throw new Exception($"TaskTypeId {booking.TaskTypeId} không tồn tại trong database");
                        }
                        
                        _context.Tasks.Add(booking);
                        await _context.SaveChangesAsync(); // Lưu booking trước để có ID cho schedule
                        
                        Console.WriteLine($"Booking created with ID: {booking.Id}");

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
                        
                        _context.WorkerSchedules.Add(schedule);
                        await _context.SaveChangesAsync();
                        
                        // Thêm thông báo cho thợ sử dụng NotificationService
                        await _notificationService.CreateBookingNotificationAsync(booking, "create");
                        
                        // Commit transaction nếu mọi thứ OK
                        await transaction.CommitAsync();
                        
                        Console.WriteLine("Transaction committed successfully");
                    }
                    catch (Exception ex)
                    {
                        // Rollback nếu có lỗi
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Error during transaction: {ex.Message}");
                        throw; // Re-throw để xử lý ở catch block bên ngoài
                    }
                }

                TempData["SuccessMessage"] = "Đặt lịch thành công! Vui lòng chờ thợ xác nhận.";
                return RedirectToAction("Details", "TaskBooking", new { id = booking.Id });
            }            catch (Exception ex)
            {
                Console.WriteLine($"Error creating booking: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Log inner exception nếu có
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    
                    // Xử lý cụ thể cho lỗi FK constraint
                    if (ex.InnerException.Message.Contains("FK_Tasks_TaskTypes_TaskTypeId"))
                    {
                        Console.WriteLine("Lỗi Foreign Key: TaskTypeId không tồn tại trong bảng TaskTypes");
                        ModelState.AddModelError("TaskTypeId", "Loại công việc không tồn tại");
                    }
                    else if (ex.InnerException.Message.Contains("FK_Tasks_Users_WorkerId"))
                    {
                        Console.WriteLine("Lỗi Foreign Key: WorkerId không tồn tại trong bảng Users");
                        ModelState.AddModelError("WorkerId", "Thợ không tồn tại");
                    }
                    else if (ex.InnerException.Message.Contains("FK_Tasks_Users_CustomerId"))
                    {
                        Console.WriteLine("Lỗi Foreign Key: CustomerId không tồn tại trong bảng Users");
                        ModelState.AddModelError("", "Người dùng không tồn tại");
                    }
                }
                
                // Hiển thị thông báo lỗi cụ thể hơn cho người dùng
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đặt lịch. Vui lòng kiểm tra thông tin và thử lại.";

                if (!ModelState.Values.Any(e => e.Errors.Count > 0))
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi đặt lịch. Vui lòng thử lại sau.");
                }
                
                model.AvailableSlots = await GetAvailableTimeSlotsForDate(model.WorkerId, model.WorkDate);
                return View(model);
            }
        }        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(int workerId, DateTime date)
        {
            var slots = await GetAvailableTimeSlotsForDate(workerId, date);
            return Json(slots);
        }
          // Phương thức để liệt kê và kiểm tra tất cả TaskTypes
        [HttpGet]
        public async Task<IActionResult> CheckTaskTypes()
        {
            var taskTypes = await _context.TaskTypes.ToListAsync();
            return Json(new { 
                Count = taskTypes.Count,
                TaskTypes = taskTypes.Select(t => new { t.Id, t.Name })
            });
        }

        // GET: TaskBooking/TaskTypeSelect
        public async Task<IActionResult> TaskTypeSelect(int workerId)
        {
            // Kiểm tra thợ có tồn tại không
            var worker = await _context.Users
                .Include(u => u.WorkerProfile)
                .ThenInclude(wp => wp.TaskType) // Sửa từ TaskTypes -> TaskType (số ít)
                .FirstOrDefaultAsync(u => u.Id == workerId);

            if (worker == null)
            {
                TempData["ErrorMessage"] = "Thợ không tồn tại";
                return RedirectToAction("Index", "Home");
            }

            if (worker.WorkerProfile == null)
            {
                TempData["ErrorMessage"] = "Thợ không có hồ sơ thợ";
                return RedirectToAction("Index", "Home");
            }

            // Một thợ chỉ có một loại công việc, chuyển thẳng đến trang tạo booking
            if (worker.WorkerProfile.TaskType != null)
            {
                return RedirectToAction("Create", new { workerId, taskTypeId = worker.WorkerProfile.TaskType.Id });
            }

            // Lấy danh sách tất cả loại công việc
            var taskTypes = await _context.TaskTypes.ToListAsync();

            ViewBag.WorkerId = workerId;
            return View(taskTypes);
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

        // GET: TaskBooking/MyBookings - Xem lịch đặt của tôi (khách hàng)
        public async Task<IActionResult> MyBookings()
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

            var bookings = await _context.Tasks
                .Where(t => t.CustomerId == userId)
                .Include(t => t.Worker)
                .Include(t => t.TaskType)
                .OrderByDescending(t => t.ScheduledTime)
                .Select(t => new TaskBookingListViewModel
                {
                    Id = t.Id,
                    WorkerName = t.Worker.FullName,
                    WorkerAvatar = t.Worker.AvatarUrl,
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

            var viewModel = new BookingsViewModel
            {
                Title = "Lịch đặt của tôi",
                Bookings = bookings
            };

            return View("Bookings", viewModel);
        }

        // GET: TaskBooking/WorkerBookings - Xem lịch hẹn (thợ)
        public async Task<IActionResult> WorkerBookings()
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

            var bookings = await _context.Tasks
                .Where(t => t.WorkerId == userId)
                .Include(t => t.Customer)
                .Include(t => t.TaskType)
                .OrderByDescending(t => t.ScheduledTime)
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

            var viewModel = new BookingsViewModel
            {
                Title = "Lịch hẹn của tôi",
                Bookings = bookings,
                IsWorkerView = true
            };

            return View("Bookings", viewModel);
        }

        // GET: TaskBooking/Details/5
        public async Task<IActionResult> Details(int id)
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

            var booking = await _context.Tasks
                .Include(t => t.Worker)
                .Include(t => t.Customer)
                .Include(t => t.TaskType)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền xem (chỉ thợ hoặc khách hàng của booking này mới được xem)
            if (booking.WorkerId != userId && booking.CustomerId != userId)
            {
                return Forbid();
            }            var viewModel = new TaskBookingDetailViewModel
            {
                Id = booking.Id,
                WorkerName = booking.Worker.FullName,
                WorkerAvatar = booking.Worker.AvatarUrl,
                WorkerId = booking.WorkerId,
                CustomerName = booking.Customer.FullName,
                CustomerAvatar = booking.Customer.AvatarUrl,
                TaskTypeName = booking.TaskType.Name,
                ScheduledTime = booking.ScheduledTime,
                Address = booking.Address,
                Description = booking.Description,
                Status = booking.Status,
                PaymentStatus = booking.PaymentStatus,
                EstimatedWork = booking.EstimatedWork,
                CreatedAt = booking.CreatedAt,
                IsWorker = booking.WorkerId == userId
            };

            return View(viewModel);
        }

        // POST: TaskBooking/UpdateStatus/5
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
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

            var booking = await _context.Tasks.FindAsync(id);

            if (booking == null)
            {
                return NotFound();
            }

            // Chỉ thợ mới được cập nhật trạng thái
            if (booking.WorkerId != userId)
            {
                return Forbid();
            }

            // Kiểm tra trạng thái hợp lệ
            if (status != "Accepted" && status != "Rejected" && status != "Completed")
            {
                return BadRequest();
            }            booking.Status = status;
            booking.UpdatedAt = DateTime.Now;
            _context.Update(booking);
            await _context.SaveChangesAsync();

            // Tạo thông báo dựa trên trạng thái mới
            string action = status.ToLower() switch
            {
                "accepted" => "accept",
                "rejected" => "reject",
                "completed" => "complete",
                _ => null
            };

            if (action != null)
            {
                await _notificationService.CreateBookingNotificationAsync(booking, action);
                
                // Thêm thông báo thành công vào TempData
                string statusMessage = status switch
                {
                    "Accepted" => "Đã chấp nhận đặt lịch thành công",
                    "Rejected" => "Đã từ chối đặt lịch",
                    "Completed" => "Đã đánh dấu công việc hoàn thành",
                    _ => "Đã cập nhật trạng thái"
                };
                
                TempData["SuccessMessage"] = statusMessage;
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}