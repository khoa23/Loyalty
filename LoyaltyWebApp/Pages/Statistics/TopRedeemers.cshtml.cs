using LoyaltyWebApp.Models;
using LoyaltyWebApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LoyaltyWebApp.Pages.Statistics
{
    public class TopRedeemersModel : PageModel
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<TopRedeemersModel> _logger;

        public TopRedeemersModel(ICustomerService customerService, ILogger<TopRedeemersModel> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        public List<CustomerStatisticViewModel> TopRedeemers { get; set; } = new List<CustomerStatisticViewModel>();

        public async Task OnGetAsync()
        {
            TopRedeemers = await _customerService.GetTopRedeemersAsync() ?? new List<CustomerStatisticViewModel>();
        }
    }
}
