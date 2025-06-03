using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SparkUp.Business;
using System;
using System.Web;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using SparkUp.MVC.Service;
using SparkUp.MVC.Models;

namespace SparkUp.MVC.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        public readonly IVnPayService _vnPayService;

        public PaymentController(AppDbContext context, IConfiguration configuration, IVnPayService vnPayService)
        {
            _context = context;
            _configuration = configuration;
            _vnPayService = vnPayService;
        }

        public IActionResult Checkout(int taskId = 0)
        {
            // Nếu chưa có task trong DB → tạo task cứng
            var task = new Business.Task
            {
                Id = 999, // ID giả để test
                Address = "123 Đường ABC, Quận 1, TP.HCM",
                ScheduledTime = DateTime.Now.AddDays(1),
                TaskType = new TaskType { Name = "Sửa chữa điện" },
                Payment = new Payment
                {
                    Amount = 500000, // 500k VND
                    PaymentTime = DateTime.Now,
                    Method = "VNPay",
                    Status = "Pending"
                }
            };

            return View(task);
        }


        [HttpPost]
        public IActionResult ProcessPayment(int taskId, string paymentMethod, decimal amount)



        {
            var vnPayModel = new VnPaymentRequestModel
            {
                Amount = (double)amount,
                CreatedDate = DateTime.Now,
                Description = $"Nạp tiền",
                FullName = "null",
                Id = new Random().Next(1000, 100000)
            };
            HttpContext.Session.SetInt32("Amount", (int)vnPayModel.Amount);
            return Redirect(_vnPayService.CreatePaymentUrl(HttpContext, vnPayModel));
        }

        public IActionResult VnpayReturn()
        {
            // Xử lý khi VNPAY trả kết quả về
            return View("Success");
        }

        public IActionResult MomoReturn()
        {
            // Demo cho MOMO
            TempData["Message"] = "Thanh toán qua MOMO thành công (DEMO)";
            return View("Success");
        }

        public IActionResult Success()
        {
            return View();
        }

    }
}
