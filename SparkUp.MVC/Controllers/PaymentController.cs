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
                // Build returnUrl / cancelUrl (nếu đã config trong appsettings.json)
                string returnUrl = _configuration["Environment:PAYOS_RETURN_URL"]
                    ?? $"{Request.Scheme}://{Request.Host}/PayOS/ReturnUrl";
                string cancelUrl = _configuration["Environment:PAYOS_CANCEL_URL"]
                    ?? $"{Request.Scheme}://{Request.Host}/PayOS/CancelUrl";

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
            task.PaymentStatus = status;
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

    }
}
