using Microsoft.AspNetCore.Mvc;

namespace SparkUp.MVC.Controllers.Admin
{
    public class AdminHomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
