using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LoyaltyWebApp.Services;
using LoyaltyWebApp.Models;

namespace LoyaltyWebApp.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly IRewardService _rewardService;
        private readonly IConfiguration _configuration;

        public IndexModel(IRewardService rewardService, IConfiguration configuration)
        {
            _rewardService = rewardService;
            _configuration = configuration;
        }

        public List<RewardItem>? Rewards { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check authentication
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
            {
                return RedirectToPage("/Login");
            }

            await LoadRewards();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string rewardId)
        {
            // Check authentication
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
            {
                return RedirectToPage("/Login");
            }

            var error = await _rewardService.DeleteRewardAsync(rewardId);

            if (error == null)
            {
                return RedirectToPage("/Admin/Index");
            }
            else
            {
                ErrorMessage = error;
            }

            await LoadRewards();
            return Page();
        }

        private async Task LoadRewards()
        {
            Rewards = await _rewardService.GetRewardsAsync();
            if (Rewards == null)
            {
                ErrorMessage = "Không thể tải danh sách quà";
            }
        }

        public string GetImageUrl(string? imageUrl)
        {
            var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:5001/";
            return _rewardService.GetImageUrl(imageUrl, apiBaseUrl);
        }
    }
}

