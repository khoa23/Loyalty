using LoyaltyWebApp.Models;
using System.Text.Json;

namespace LoyaltyWebApp.Services
{
    public interface ICustomerService
    {
        Task<List<CustomerModel>?> GetCustomersAsync();
    }

    public class CustomerService : ICustomerService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<CustomerService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<CustomerModel>?> GetCustomersAsync()
        {
            try
            {
                var webAppUrl = _configuration["WebAppUrl"];
                if (string.IsNullOrEmpty(webAppUrl))
                {
                    _logger.LogError("❌ WebAppUrl is not configured in appsettings.json");
                    return null;
                }
                
                if (!webAppUrl.EndsWith("/")) webAppUrl += "/";
                
                var requestUrl = $"{webAppUrl}Customer/GetCustomers";
                
                _logger.LogInformation("📡 Calling GetCustomers endpoint from: {RequestUrl}", requestUrl);
                
                var response = await _httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    
                    using var document = JsonDocument.Parse(jsonContent);
                    var root = document.RootElement;
                    
                    if (root.TryGetProperty("data", out var dataElement))
                    {
                        var customers = JsonSerializer.Deserialize<List<CustomerModel>>(dataElement.GetRawText(), options);
                        _logger.LogInformation("✓ Successfully fetched {Count} customers", customers?.Count ?? 0);
                        return customers;
                    }
                    
                    return null;
                }
                else
                {
                    _logger.LogWarning("✗ API returned status code {StatusCode}", response.StatusCode);
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Response content: {Content}", content);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error loading customers");
                return null;
            }
        }
    }
}
