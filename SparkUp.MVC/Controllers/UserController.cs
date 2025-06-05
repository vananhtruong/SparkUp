using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SparkUp.Business;
using SparkUp.MVC.Models;

namespace SparkUp.MVC.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Profile()
        {
            // Lấy userId từ claims (khi đã đăng nhập)
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Authentication");
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users
                .Include(u => u.WorkerProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var userProfileViewModel = new UserProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                IsWorker = user.WorkerProfile != null
            };

            return View(userProfileViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(UserProfileViewModel model)
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

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Cập nhật thông tin người dùng
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            // Email thường không được cập nhật dễ dàng vì đây có thể là thông tin đăng nhập
            // Nếu muốn cập nhật email, nên có quy trình xác minh

            // Cập nhật avatar nếu có
            if (!string.IsNullOrEmpty(model.NewAvatarUrl))
            {
                user.AvatarUrl = model.NewAvatarUrl;
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công";
            return RedirectToAction(nameof(Profile));
        }
    }
}
