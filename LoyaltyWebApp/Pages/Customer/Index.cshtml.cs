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
        public PaginatedRewardResult? PaginatedRewards { get; set; }
        public long CurrentPoints { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 1;
        public IConfiguration Configuration => _configuration;

        public async Task<IActionResult> OnGetAsync(int page = 1)
        {
            // Check authentication
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Customer")
            {
                return RedirectToPage("/Login");
            }

            CurrentPage = page < 1 ? 1 : page;
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

            if (long.TryParse(rewardId, out var rewardIdLong))
            {
                var customerIdStr = HttpContext.Session.GetString("CustomerId");
                if (long.TryParse(customerIdStr, out var customerId))
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
                    ErrorMessage = "Thông tin khách hàng không hợp lệ";
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
            PaginatedRewards = await _rewardService.GetCustomerRewardsWithPaginationAsync(CurrentPage, PageSize);
            if (PaginatedRewards != null)
            {
                Rewards = PaginatedRewards.Data;
            }
            else
            {
                ErrorMessage = "Không thể tải danh sách quà";
                Rewards = new List<RewardItem>();
            }
        }

        private async Task LoadCustomerInfo(string userId)
        {
            if (long.TryParse(userId, out var userIdLong))
            {
                var customerInfo = await _userService.GetCustomerInfoAsync(userIdLong);
                CurrentPoints = customerInfo?.Current_Points ?? 0;
            }
        }

        public string GetImageUrl(string? imageUrl)
        {
            var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:5001/";
            return _rewardService.GetImageUrl(imageUrl, apiBaseUrl);
        }
    }
}

