using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LoyaltyWebApp.Services;

namespace LoyaltyWebApp.Pages.Admin
{
    public class CreateModel : PageModel
    {
        private readonly IRewardService _rewardService;

        public CreateModel(IRewardService rewardService)
        {
            _rewardService = rewardService;
        }

        public string? ErrorMessage { get; set; }
        public string? UserId { get; set; }

        public IActionResult OnGet()
        {
            // Check authentication
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
            {
                return RedirectToPage("/Login");
            }

            UserId = userId;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            string rewardName,
            string? description,
            long pointsCost,
            int stockQuantity,
            bool isActive,
            IFormFile? imageFile)
        {
            // Check authentication
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
            {
                return RedirectToPage("/Login");
            }

            UserId = userId;

            long? lastUpdatedBy = long.TryParse(userId, out var uid) ? uid : null;
            var error = await _rewardService.CreateRewardAsync(rewardName, description, pointsCost, stockQuantity, isActive, lastUpdatedBy, imageFile);

            if (error == null)
            {
                return RedirectToPage("/Admin/Index");
            }
            else
            {
                ErrorMessage = error;
            }

            return Page();
        }
    }
}

