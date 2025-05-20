using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SparkUp.MVC.Controllers
{
    public class AuthenticationController : Controller
    {
        //return to login page
        public IActionResult Index()
        {
            return View();
        }

        //Login User
        public IActionResult Login()
        {
            var claims = new Claim[]
            {
                new Claim(ClaimTypes.Name, "User"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            return RedirectToAction("Index","Home");
        }

        //register with new account
        public IActionResult Register()
        {
            return View();
        }

        //change to forget password
        public IActionResult ForgotPassword()
        {
            return View();
        }

        public IActionResult WorkerRegister()
        {
            return View();
        }
    }
}
