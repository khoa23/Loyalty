using LoyaltyWebApp.Models;
using LoyaltyWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoyaltyWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole == "Admin")
                {
                    return RedirectToAction("Index", "Admin"); 
                }
                else
                {
                    return RedirectToAction("Index", "Customer"); // Same logic
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ thông tin";
                return View(model);
            }

            var response = await _userService.LoginAsync(model);

            if (response != null)
            {
                HttpContext.Session.SetString("UserId", response.User_Id.ToString());
                HttpContext.Session.SetString("Username", response.Username);
                HttpContext.Session.SetString("UserRole", response.User_Role);
                if (response.Customer_Id.HasValue)
                {
                    HttpContext.Session.SetString("CustomerId", response.Customer_Id.Value.ToString());
                }

                if (response.User_Role == "Admin")
                {
                    // Check if we can redirect to Razor Page if valid
                    return Redirect("/Admin/Index"); 
                    // Note: If Admin/Index is Razor Page, we can redirect by path.
                }
                else
                {
                    return Redirect("/Customer/Index");
                }
            }

            ViewBag.ErrorMessage = "Sai tên đăng nhập hoặc mật khẩu";
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                 var userRole = HttpContext.Session.GetString("UserRole");
                 return Redirect(userRole == "Admin" ? "/Admin" : "/Customer");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ thông tin bắt buộc";
                return View(model);
            }

            var error = await _userService.RegisterAsync(model);

            if (error == null)
            {
                ViewBag.SuccessMessage = "Đăng ký thành công! Vui lòng đăng nhập.";
                ModelState.Clear();
                return View();
            }

            ViewBag.ErrorMessage = error;
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
