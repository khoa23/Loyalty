using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LoyaltyWebApp.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Login");
            }

            // Redirect based on role
            if (userRole == "Admin")
            {
                return RedirectToPage("/Admin/Index");
            }
            else if (userRole == "Customer")
            {
                return RedirectToPage("/Customer/Index");
            }

            return RedirectToPage("/Login");
        }
    }
}
