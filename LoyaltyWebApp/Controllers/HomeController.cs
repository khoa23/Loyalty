using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace LoyaltyWebApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Redirect based on role
            if (userRole == "Admin")
            {
               return Redirect("/Admin/Index");
            }
            else if (userRole == "Customer")
            {
                return Redirect("/Customer/Index");
            }

            return RedirectToAction("Login", "Account");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
