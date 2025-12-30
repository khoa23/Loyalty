using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LoyaltyWebApp.Services;
using LoyaltyWebApp.Models;

namespace LoyaltyWebApp.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IUserService _userService;

        public RegisterModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        public string? PhoneNumber { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

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
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(FullName))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ thông tin bắt buộc";
                return Page();
            }

            var registerRequest = new RegisterRequest
            {
                Username = Username,
                Password = Password,
                FullName = FullName,
                PhoneNumber = PhoneNumber
            };

            var error = await _userService.RegisterAsync(registerRequest);

            if (error == null)
            {
                SuccessMessage = "Đăng ký thành công! Vui lòng đăng nhập.";
                // Clear form
                Username = string.Empty;
                Password = string.Empty;
                FullName = string.Empty;
                PhoneNumber = null;
            }
            else
            {
                ErrorMessage = error;
            }

            return Page();
        }
    }
}

