using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LoyaltyWebApp.Models;
using LoyaltyWebApp.Services;

namespace LoyaltyWebApp.Pages.Admin.Customers
{
    public class IndexModel : PageModel
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ICustomerService customerService, ILogger<IndexModel> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        public List<CustomerModel>? Customers { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check authentication
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
            {
                return RedirectToPage("/Login");
            }

            await LoadCustomers();
            return Page();
        }

        private async Task LoadCustomers()
        {
            Customers = await _customerService.GetCustomersAsync();
            
            if (Customers == null)
            {
                ErrorMessage = "Không thể tải danh sách khách hàng từ máy chủ";
                Customers = new List<CustomerModel>();
            }
        }
    }
}
