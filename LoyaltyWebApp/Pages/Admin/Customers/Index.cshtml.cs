using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LoyaltyWebApp.Models;
using System.Net.Http;
using System.Text.Json;

namespace LoyaltyWebApp.Pages.Admin.Customers
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public IndexModel(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
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
            try
            {
                var apiUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
                var response = await _httpClient.GetAsync($"{apiUrl}/api/customer/list");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    Customers = JsonSerializer.Deserialize<List<CustomerModel>>(jsonContent, options);
                }
                else
                {
                    ErrorMessage = "Không thể tải danh sách khách hàng từ máy chủ";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải danh sách khách hàng: {ex.Message}";
            }
        }
    }
}
