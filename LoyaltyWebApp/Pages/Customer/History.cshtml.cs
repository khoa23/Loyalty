using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LoyaltyWebApp.Services;
using LoyaltyWebApp.Models;

namespace LoyaltyWebApp.Pages.Customer
{
    public class HistoryModel : PageModel
    {
        private readonly ILoyaltyService _loyaltyService;

        public HistoryModel(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService;
        }

        public List<HistoryItem>? History { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check authentication
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Customer")
            {
                return RedirectToPage("/Login");
            }

            await LoadHistory(userId);
            return Page();
        }

        private async Task LoadHistory(string userId)
        {
            var customerIdStr = HttpContext.Session.GetString("CustomerId");
            if (long.TryParse(customerIdStr, out var customerId))
            {
                History = await _loyaltyService.GetRedemptionHistoryAsync(customerId);
                if (History == null)
                {
                    ErrorMessage = "Không thể tải lịch sử đổi quà";
                }
            }
            else
            {
                ErrorMessage = "Thông tin khách hàng không hợp lệ";
            }
        }
    }
}

