using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LoyaltyWebApp.Services;
using LoyaltyWebApp.Models;

namespace LoyaltyWebApp.Pages.Customer
{
    public class IndexModel : PageModel
    {
        private readonly IRewardService _rewardService;
        private readonly ILoyaltyService _loyaltyService;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        public IndexModel(IRewardService rewardService, ILoyaltyService loyaltyService, IUserService userService, IConfiguration configuration)
        {
            _rewardService = rewardService;
            _loyaltyService = loyaltyService;
            _userService = userService;
            _configuration = configuration;
        }

        public List<RewardItem>? Rewards { get; set; }
        public long CurrentPoints { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check authentication
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Customer")
            {
                return RedirectToPage("/Login");
            }

            await LoadCustomerInfo(userId);
            await LoadRewards();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string rewardId, int quantity)
        {
            // Check authentication
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Customer")
            {
                return RedirectToPage("/Login");
            }

            if (long.TryParse(userId, out var customerId) && long.TryParse(rewardId, out var rewardIdLong))
            {
                var error = await _loyaltyService.RedeemRewardAsync(customerId, rewardIdLong, quantity);

                if (error == null)
                {
                    SuccessMessage = "Đổi quà thành công!";
                }
                else
                {
                    ErrorMessage = error;
                }
            }
            else
            {
                ErrorMessage = "Thông tin không hợp lệ";
            }

            await LoadCustomerInfo(userId);
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

        private async Task LoadCustomerInfo(string userId)
        {
            if (long.TryParse(userId, out var userIdLong))
            {
                var points = await _userService.GetCustomerPointsAsync(userIdLong);
                CurrentPoints = points ?? 0;
            }
        }

        public string GetImageUrl(string? imageUrl)
        {
            var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:5001/";
            return _rewardService.GetImageUrl(imageUrl, apiBaseUrl);
        }
    }
}

