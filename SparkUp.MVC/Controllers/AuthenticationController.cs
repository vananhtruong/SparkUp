using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SparkUp.Business;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SparkUp.MVC.Service;
using System.Security.Cryptography;
using System.Web;
using SparkUp.MVC.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SparkUp.MVC.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly SettingsDto _settings;
        private readonly IMemoryCache _memoryCache;

        public AuthenticationController(AppDbContext context, IEmailSender emailSender, IOptions<SettingsDto> settings, IMemoryCache memoryCache)
        {
            _context = context;
            _emailSender = emailSender;
            _settings = settings.Value;
            _memoryCache = memoryCache;
        }

        //return to login page
        public IActionResult Index()
        {
            return View();
        }        //Login User
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(email));            if(user != null && user.PasswordHash.Equals(password)) { 
                var claims = new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("AvatarUrl", user.AvatarUrl ?? "")
                };

                // Tạo ClaimsIdentity
                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                
                // Đăng nhập người dùng
                await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));
                
                // Chuyển hướng dựa vào vai trò
                if (user.Role == "ADMIN")
                {
                    return Redirect("https://localhost:7261/AdminHome/Index");
                }
                
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("Index");
        }

        public async System.Threading.Tasks.Task GoogleLogin()
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, new AuthenticationProperties()
            {
                RedirectUri = Url.Action("GoogleResponse")
            });
        }        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return RedirectToAction("Index");

            //find user
            var principal = result.Principal;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(email));
            Random rand = new Random();
            
            if(user == null)
            {
                var newUser = new User()
                {
                    FullName = principal.FindFirst(ClaimTypes.Name)?.Value,
                    Email = principal.FindFirst(ClaimTypes.Email)?.Value,
                    Role = "Customer",
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    AvatarUrl = "default.png",
                    PasswordHash = (rand.Next(100000, 999999)).ToString() + (char)('a' + rand.Next(0, 26)) + (char)('A' + rand.Next(0, 26)),
                    PhoneNumber = "not yet"
                };
                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
                user = newUser;
            }            //create claim for user log in cookie
            var claims = new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("AvatarUrl", user.AvatarUrl ?? "")
                };
                
            // Tạo ClaimsIdentity
            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            
            // Đăng nhập người dùng
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

            // Chuyển hướng dựa vào vai trò
            if (user.Role == "ADMIN")
            {
                return Redirect("https://localhost:7261/AdminHome/Index");
            }

            return RedirectToAction("Index", "Home");
        }

        public async System.Threading.Tasks.Task FacebookLogin()
        {
            await HttpContext.ChallengeAsync(FacebookDefaults.AuthenticationScheme, new AuthenticationProperties()
            {
                RedirectUri = Url.Action("FacebookResponse")
            });
        }        public async Task<IActionResult> FacebookResponse()
        {
            var result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);

            if (result?.Succeeded != true)
            {
                return RedirectToAction("Index");
            }

            // Get user info from Facebook
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var id = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Không thể lấy thông tin email từ Facebook";
                return RedirectToAction("Index");
            }

            // Kiểm tra user đã tồn tại chưa
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(email));
            Random rand = new Random();
            
            if(user == null)
            {
                var newUser = new User()
                {
                    FullName = name ?? "Facebook User",
                    Email = email,
                    Role = "Customer",
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    AvatarUrl = "default.png",
                    PasswordHash = (rand.Next(100000, 999999)).ToString() + (char)('a' + rand.Next(0, 26)) + (char)('A' + rand.Next(0, 26)),
                    PhoneNumber = "not yet"
                };
                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
                user = newUser;
            }

            // Tạo claims và đăng nhập
            var claims = new Claim[]
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("AvatarUrl", user.AvatarUrl ?? "")
            };
              // Tạo ClaimsIdentity
            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            
            // Đăng nhập người dùng
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));
            
            // Chuyển hướng dựa vào vai trò
            if (user.Role == "ADMIN")
            {
                return Redirect("https://localhost:7261/AdminHome/Index");
            }
            
            return RedirectToAction("Index", "Home");
        }        //register with new account
        public async Task<IActionResult> Register(string fullName, string email, string phone, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(email));
            if(user == null)
            {
                User newUser = new User()
                {
                    FullName = fullName,
                    Email = email,
                    PhoneNumber = phone,
                    PasswordHash = password,
                    Role = "Customer",
                    AvatarUrl = "default.png",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
                
                // Đăng nhập người dùng sau khi đăng ký
                var claims = new Claim[]
                {
                    new Claim(ClaimTypes.Name, newUser.FullName),
                    new Claim(ClaimTypes.Role, newUser.Role),
                    new Claim(ClaimTypes.Email, newUser.Email),
                    new Claim(ClaimTypes.NameIdentifier, newUser.Id.ToString()),
                    new Claim("AvatarUrl", newUser.AvatarUrl ?? "")
                };
                
                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));
                
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index");
        }

        //change to forget password
        public IActionResult ForgotPasswordView()
        {
            return View("ForgotPassword");

        }

        public IActionResult ForgotPassword(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email.Equals(email));
            if (user != null)
            {
                //var template = _context.EmailTemplates.FirstOrDefault(t => t.Purpose.Equals("ForgotPassword"));
                //create link for reset password
                var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

                //store token in cache
                _memoryCache.Set(rawToken, user.Id, TimeSpan.FromMinutes(_settings.ExpiredCache));

                //create link
                var link = $"{_settings.Domain}Authentication/ResetPasswordView?token={rawToken}";

                //send email
                //_emailSender.SendEmailAsync(user.Email, template.Header, template.Body.Replace("{link}", link));
            }
            return RedirectToAction("ForgotPasswordView");
        }

        public IActionResult ResetPasswordView(string token)
        {
            if (token != null && _memoryCache.TryGetValue(token, out var userId))
            {
                ViewData["UserId"] = userId;
                return View("ResetPassword");
            }
            return RedirectToAction("ForgotPasswordView");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult ResetPassword(int UserId, string password)
        {
            var user = _context.Users.Find(UserId);
            if (user != null)
            {
                user.PasswordHash = password;
                _context.Users.Entry(user).State = EntityState.Modified;
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }        public async Task<IActionResult> WorkerRegister()
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

            // Kiểm tra xem người dùng đã là thợ chưa
            var user = await _context.Users.Include(u => u.WorkerProfile).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound();
            }

            if (user.WorkerProfile != null)
            {
                // Người dùng đã là thợ, chuyển hướng đến trang hồ sơ thợ
                return RedirectToAction("Profile", "Worker", new { id = userId });
            }

            // Lấy danh sách loại công việc cho dropdown
            ViewBag.TaskTypes = await _context.TaskTypes
                .Where(tt => tt.IsActive)
                .Select(tt => new SelectListItem
                {
                    Value = tt.Id.ToString(),
                    Text = tt.Name
                })
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> WorkerRegister(WorkerRegistrationViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.TaskTypes = await _context.TaskTypes
                    .Where(tt => tt.IsActive)
                    .Select(tt => new SelectListItem
                    {
                        Value = tt.Id.ToString(),
                        Text = tt.Name
                    })
                    .ToListAsync();
                return View(model);
            }

            // Tạo hồ sơ thợ mới
            var workerProfile = new WorkerProfile
            {
                UserId = userId,
                Skills = model.Skills,
                Description = model.Description,
                ExperienceYears = model.ExperienceYears,
                Address = model.Address,
                City = model.City,
                District = model.District,
                TaskTypeId = model.TaskTypeId,
                HourlyRate = model.HourlyRate,
                IsConfirmed = false,
                ApprovalStatus = "Pending",
                RatingAverage = 0
            };

            _context.WorkerProfiles.Add(workerProfile);
            
            // Cập nhật vai trò người dùng
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Role = "Worker";
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();            // Cập nhật claim người dùng
            var identity = new ClaimsIdentity(User.Identity);
            identity.RemoveClaim(identity.FindFirst(ClaimTypes.Role));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Worker"));
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(identity));

            TempData["SuccessMessage"] = "Đăng ký làm thợ thành công! Hồ sơ của bạn đang được xét duyệt.";
            return RedirectToAction("Profile", "Worker", new { id = userId });
        }        // Đăng xuất người dùng
        public async Task<IActionResult> Logout()
        {
            // Clear authentication cookie
            await HttpContext.SignOutAsync("CookieAuth");
            
            // Store a temporary message
            TempData["LogoutMessage"] = "Bạn đã đăng xuất thành công";
            
            // Redirect to home page
            return RedirectToAction("Index", "Home");
        }
    }
}
