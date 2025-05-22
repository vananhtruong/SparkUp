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

namespace SparkUp.MVC.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly SettingsDto _settings;
        private readonly IMemoryCache _memoryCache;

        public AuthenticationController(AppDbContext context, IEmailSender emailSender, SettingsDto settings, IMemoryCache memoryCache)
        {
            _context = context;
            _emailSender = emailSender;
            _settings = settings;
            _memoryCache = memoryCache;
        }

        //return to login page
        public IActionResult Index()
        {
            return View();
        }

        //Login User
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(email));
            if(user != null && user.PasswordHash.Equals(password)) { 
                var claims = new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                };
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
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims.Select(c => new
            {
                c.Issuer,
                c.OriginalIssuer,
                c.Type,
                c.Value
            });

            return Json(claims);
        }

        public async System.Threading.Tasks.Task FacebookLogin()
        {
            await HttpContext.ChallengeAsync(FacebookDefaults.AuthenticationScheme, new AuthenticationProperties()
            {
                RedirectUri = Url.Action("FacebookResponse")
            });
        }

        public async Task<IActionResult> FacebookResponse()
        {
            var result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);

            if (result?.Succeeded != true)
            {
                return RedirectToAction("Index");
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims.Select(c => new
            {
                c.Issuer,
                c.OriginalIssuer,
                c.Type,
                c.Value
            });

            // Get user info from Facebook
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var id = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Here you would typically:
            // 1. Check if user exists in your database
            // 2. If not, create new user
            // 3. Create authentication cookie
            // 4. Redirect to home page

            // For now, we'll just return the claims
            return Json(claims);
        }

        //register with new account
        public async Task<IActionResult> RegisterAsync(string fullName, string email, string phone, string password)
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
            }

            return RedirectToAction("Index");
        }

        //change to forget password
        public IActionResult ForgotPasswordView()
        {
            return View();

        }

        public IActionResult ForgotPassword(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email.Equals(email));
            if (user != null)
            {
                var template = _context.EmailTemplates.FirstOrDefault(t => t.Purpose.Equals("ForgotPassword"));
                //create link for reset password
                var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                var encodedToken = HttpUtility.UrlEncode(rawToken);

                //store token in cache
                _memoryCache.Set(encodedToken, user.Id, TimeSpan.FromMinutes(_settings.ExpiredCache));

                //create link
                var link = $"{_settings.Domain}ResetPasswordView?token={encodedToken}";

                //send email
                _emailSender.SendEmailAsync(user.Email, template.Header, template.Body.Replace("{link}", link));
            }
            return RedirectToAction("ForgotPasswordView");
        }

        public IActionResult ResetPasswordView(string token)
        {
            if (_memoryCache.TryGetValue(token, out var userId))
            {
                return View();
                ViewData["UserId"] = userId;
                return RedirectToAction("Index");
            }
            return RedirectToAction("ForgotPasswordView");
        }

        [ValidateAntiForgeryToken]
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
        }

        public IActionResult WorkerRegister()
        {
            return View();
        }
    }
}
