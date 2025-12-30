using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LoyaltyWebApp.Services;
using LoyaltyWebApp.Models;

namespace LoyaltyWebApp.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;

        public LoginModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // Nếu đã đăng nhập, redirect về trang chính
            if (HttpContext.Session.GetString("UserId") != null)
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole == "Admin")
                {
                    Response.Redirect("/Admin");
                }
                else
                {
                    Response.Redirect("/Customer");
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ thông tin";
                return Page();
            }

            var loginRequest = new LoginRequest
            {
                Username = Username,
                Password = Password
            };

            var loginResponse = await _userService.LoginAsync(loginRequest);

            if (loginResponse != null)
            {
                // Lưu thông tin vào session
                HttpContext.Session.SetString("UserId", loginResponse.User_Id.ToString());
                HttpContext.Session.SetString("Username", loginResponse.Username);
                HttpContext.Session.SetString("UserRole", loginResponse.User_Role);

                // Redirect theo role
                if (loginResponse.User_Role == "Admin")
                {
                    return RedirectToPage("/Admin/Index");
                }
                else
                {
                    return RedirectToPage("/Customer/Index");
                }
            }
            else
            {
                ErrorMessage = "Sai tên đăng nhập hoặc mật khẩu";
            }

            return Page();
        }
    }
}

