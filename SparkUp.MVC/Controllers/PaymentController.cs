using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Net.payOS;
using Net.payOS.Types;
using SparkUp.Business;
using System.Threading.Tasks;

namespace SparkUp.MVC.Controllers
{
    public class PaymentController : Controller
    {
        private readonly PayOS _payOS;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(PayOS payOS, AppDbContext context, IConfiguration configuration)
        {
            _payOS = payOS ?? throw new ArgumentNullException(nameof(payOS));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration;
        }
        [HttpPost]
        public async Task<IActionResult> Pay(int taskId)
        {
            var task = await _context.Tasks.Include(t => t.Payment).Include(t => t.Customer).FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
            {
                TempData["Error"] = "Không tìm thấy đơn đặt lịch.";
                return RedirectToAction("MyBookings", "TaskBooking");
            }

            if (task.PaymentStatus == "Paid")
            {
                TempData["Message"] = "Đơn hàng đã được thanh toán.";
                return RedirectToAction("Details", "TaskBooking", new { id = taskId });
            }

            decimal amount = 2000m; // Gán cố định 2000 VND

            // Tạo orderCode kiểu long duy nhất
            long uniquePart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1_000_000; // 6 chữ số cuối
            long orderCodeLong = task.Id * 1_000_000 + uniquePart;

            try
            {
                string returnUrl = $"{Request.Scheme}://{Request.Host}/Payment/ReturnUrl";
                string cancelUrl = $"{Request.Scheme}://{Request.Host}/Payment/CancelUrl";

                var paymentData = new PaymentData(
                orderCode: orderCodeLong,
                amount: (int)amount,
                description: $"Thanh toán đơn số {task.Id}",
                items: null,
                cancelUrl: cancelUrl,
                returnUrl: returnUrl
            );


                CreatePaymentResult createPayment = await _payOS.createPaymentLink(paymentData);

                // Trong CreatePaymentResult, URL thanh toán nằm ở property `checkoutUrl` (camelCase)
                if (createPayment == null || string.IsNullOrEmpty(createPayment.checkoutUrl))
                {
                    ModelState.AddModelError("", "Không thể tạo link thanh toán, vui lòng thử lại.");
                    return View();
                }
                
                return Redirect(createPayment.checkoutUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tạo đơn thanh toán: {ex.Message}";
                return RedirectToAction("Details", "TaskBooking", new { id = taskId });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ReturnUrl(string status, string orderCode, string paymentId, string signature)
        {
            if (!long.TryParse(orderCode, out long orderCodeLong))
            {
                TempData["Error"] = "Order code không hợp lệ";
                return RedirectToAction("MyBookings", "TaskBooking");
            }

            int taskId = (int)(orderCodeLong / 1_000_000);
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
            task.PaymentStatus = "Paid";
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();

            


            return RedirectToAction("Details", "TaskBooking", new { id = task.Id });
        }


        // GET: /PayOS/CancelUrl
        [HttpGet]
        public async Task<IActionResult> CancelUrl(string orderCode)
        {
            if (!long.TryParse(orderCode, out long orderCodeLong))
            {
                TempData["Error"] = "Order code không hợp lệ";
                return RedirectToAction("MyBookings", "TaskBooking");
            }

            int taskId = (int)(orderCodeLong / 1_000_000);
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
            ViewBag.Message = "Bạn đã hủy giao dịch hoặc thanh toán không thành công.";
            return RedirectToAction("Details", "TaskBooking", new { id = task.Id });
        }

        [HttpPost]
        public async Task<IActionResult> PayWithWallet(int taskId)
        {
            // Lấy user hiện tại
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                TempData["Error"] = "Không xác định được người dùng.";
                return RedirectToAction("Details", "TaskBooking", new { id = taskId });
            }
            int uid = int.Parse(userId);

            // Tìm task booking
            var task = await _context.Tasks
                .Include(t => t.Customer)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                TempData["Error"] = "Không tìm thấy đơn đặt lịch.";
                return RedirectToAction("MyBookings", "TaskBooking");
            }

            if (task.PaymentStatus == "Paid")
            {
                TempData["SuccessMessage"] = "Đơn hàng đã được thanh toán.";
                return RedirectToAction("Details", "TaskBooking", new { id = taskId });
            }

            // Số tiền cần thanh toán (demo: 2000, thực tế lấy từ task.Price hoặc field tương ứng)
            decimal amount = 2000m;

            // Tìm ví khách hàng
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == uid);
            if (wallet == null || wallet.IsLocked)
            {
                TempData["Error"] = "Ví của bạn không khả dụng.";
                return RedirectToAction("Details", "TaskBooking", new { id = taskId });
            }

            if (wallet.Balance < amount)
            {
                TempData["Error"] = "Số dư ví không đủ để thanh toán.";
                return RedirectToAction("Details", "TaskBooking", new { id = taskId });
            }

            // Trừ tiền
            wallet.Balance -= amount;
            wallet.UpdatedAt = DateTime.Now;

            // Tạo lịch sử giao dịch
            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = -amount,
                Type = "Payment",
                TaskId = taskId,
                Description = $"Thanh toán đơn đặt lịch #{taskId}",
                CreatedAt = DateTime.Now,
                Status = "Success"
            };
            _context.WalletTransactions.Add(transaction);

            // Đánh dấu đơn đã thanh toán
            task.PaymentStatus = "Paid";
            _context.Tasks.Update(task);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thanh toán qua ví thành công!";
            return RedirectToAction("Details", "TaskBooking", new { id = taskId });
        }


    }
}
