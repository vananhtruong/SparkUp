using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SparkUp.MVC.Controllers.Admin
{
    [Authorize(Roles = "ADMIN")]
    public class AdminHomeController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Admin/AdminHome/Index.cshtml");
        }
        
        public IActionResult Settings()
        {
            return View("~/Views/Admin/AdminHome/Settings.cshtml");
        }
    }
}
