using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LoyaltyWebApp.Services;
using LoyaltyWebApp.Models;

namespace LoyaltyWebApp.Pages.Admin
{
    public class EditModel : PageModel
    {
        private readonly IRewardService _rewardService;

        public EditModel(IRewardService rewardService)
        {
            _rewardService = rewardService;
        }

        public RewardItem? Reward { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UserId { get; set; }

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            // Check authentication
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
            {
                return RedirectToPage("/Login");
            }

            UserId = userId;

            if (string.IsNullOrEmpty(id))
            {
                ErrorMessage = "Không tìm thấy ID quà";
                return Page();
            }

            await LoadReward(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            string rewardId,
            string rewardName,
            string? description,
            long pointsCost,
            int stockQuantity,
            bool isActive,
            long? lastUpdatedBy)
        {
            // Check authentication
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
            {
                return RedirectToPage("/Login");
            }

            UserId = userId;

            long? lastUpdatedByValue = long.TryParse(userId, out var uid) ? uid : null;
            var error = await _rewardService.UpdateRewardAsync(rewardId, rewardName, description, pointsCost, stockQuantity, isActive, lastUpdatedByValue);

            if (error == null)
            {
                return RedirectToPage("/Admin/Index");
            }
            else
            {
                ErrorMessage = error;
            }

            await LoadReward(rewardId);
            return Page();
        }

        private async Task LoadReward(string id)
        {
            Reward = await _rewardService.GetRewardByIdAsync(id);
            if (Reward == null)
            {
                ErrorMessage = "Không thể tải thông tin quà";
            }
        }
    }
}

