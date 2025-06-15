using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Net.payOS;
using Net.payOS.Types;
using SparkUp.Business;
using SparkUp.MVC.Models;
using System.Security.Claims;

namespace SparkUp.MVC.Controllers
{
    public class WalletController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PayOS _payOS;
        private readonly IConfiguration _configuration;

        public WalletController(AppDbContext context, PayOS payOS, IConfiguration configuration)
        {
            _context = context;
            _payOS = payOS;
            _configuration = configuration;
        }

        // GET: /Wallet/TopUp
        [HttpGet]
        public IActionResult TopUp()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopUp(TopUpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                TempData["Error"] = "Không xác định được người dùng.";
                return RedirectToAction("TopUp");
            }

            // Tìm hoặc tạo ví
            int uid = int.Parse(userId);
            var userWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == uid);
            if (userWallet == null)
            {
                userWallet = new Wallet
                {
                    UserId = uid,
                    Balance = 0,
                    IsLocked = false,
                    UpdatedAt = DateTime.Now
                };
                _context.Wallets.Add(userWallet);
                await _context.SaveChangesAsync();
            }

            // 1. Tạo WalletTransaction trước khi gọi PayOS
            var transaction = new WalletTransaction
            {
                WalletId = userWallet.Id,
                Amount = model.Amount.Value,
                Type = "Deposit",
                Description = $"Nạp tiền qua PayOS",
                CreatedAt = DateTime.Now,
                Status = "Pending"
            };
            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // 2. Mã hóa orderCode từ Id của transaction (ví dụ: TransactionId * 1000 + random 3 số)
            var rand = new Random();
            int salt = rand.Next(1, 1000);
            long orderCode = transaction.Id * 1000L + salt;

            // 3. Lưu lại salt vào Description (tùy bạn muốn, hoặc không cần)
            transaction.Description += $"|salt:{salt}";
            await _context.SaveChangesAsync();

            string returnUrl = $"{Request.Scheme}://{Request.Host}/Wallet/TopUpReturn";
            string cancelUrl = $"{Request.Scheme}://{Request.Host}/Wallet/TopUpCancel";

            try
            {
                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: (int)model.Amount.Value,
                    description: $"Nạp ví - Mã GD #{transaction.Id}",
                    items: null,
                    cancelUrl: cancelUrl,
                    returnUrl: returnUrl
                );

                CreatePaymentResult createPayment = await _payOS.createPaymentLink(paymentData);

                if (createPayment == null || string.IsNullOrEmpty(createPayment.checkoutUrl))
                {
                    ModelState.AddModelError("", "Không thể tạo link thanh toán, vui lòng thử lại.");
                    return View(model);
                }
                // Redirect sang trang thanh toán PayOS
                return Redirect(createPayment.checkoutUrl);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi khi tạo đơn nạp tiền: " + ex.Message);
                return View(model);
            }
        }


        // GET: /Wallet/TopUpReturn
        [HttpGet]
        public async Task<IActionResult> TopUpReturn(string status, string orderCode, string paymentId, string signature)
        {
            if (!long.TryParse(orderCode, out long code))
            {
                TempData["Error"] = "Order code không hợp lệ";
                return RedirectToAction("TopUp");
            }

            // Giải mã transactionId từ orderCode
            int transactionId = (int)(code / 1000);

            var transaction = await _context.WalletTransactions
                .Include(t => t.Wallet)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                TempData["Error"] = "Không tìm thấy giao dịch nạp tiền.";
                return RedirectToAction("TopUp");
            }

            if (transaction.Status == "Success")
            {
                TempData["Success"] = "Nạp tiền thành công!";
                return RedirectToAction("TopUp");
            }

            if (status == "PAID" || status == "SUCCEEDED" || status?.ToLower() == "success")
            {
                // Cộng tiền vào ví
                transaction.Wallet.Balance += transaction.Amount;
                transaction.Wallet.UpdatedAt = DateTime.Now;
                transaction.Status = "Success";
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Nạp {transaction.Amount:N0}đ thành công!";
                return RedirectToAction("TopUp");
            }
            else
            {
                transaction.Status = "Failed";
                await _context.SaveChangesAsync();

                TempData["Error"] = "Thanh toán không thành công hoặc đã bị hủy.";
                return RedirectToAction("TopUp");
            }
        }



        // GET: /Wallet/TopUpCancel
        [HttpGet]
        public async Task<IActionResult> TopUpCancel(string orderCode)
        {
            if (long.TryParse(orderCode, out long code))
            {
                int transactionId = (int)(code / 1000);
                var transaction = await _context.WalletTransactions.FindAsync(transactionId);
                if (transaction != null && transaction.Status != "Success")
                {
                    transaction.Status = "Failed";
                    await _context.SaveChangesAsync();
                }
            }
            TempData["Error"] = "Bạn đã hủy giao dịch hoặc thanh toán không thành công.";
            return RedirectToAction("TopUp");
        }

    }
}
