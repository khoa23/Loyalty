using LoyaltyWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LoyaltyWebApp.Controller
{
    [Route("Customer")]
    public class CustomerController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<CustomerController> logger)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        [HttpGet("GetCustomers")]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:44343/";
                var apiKey = _configuration.GetValue<string>("ApiKey");

                if (!apiBaseUrl.EndsWith("/")) apiBaseUrl += "/";

                var requestUrl = $"{apiBaseUrl}api/customer/list";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Add("X-API-KEY", apiKey);
                    _logger.LogInformation("📡 API Key added to request header");
                }

                _logger.LogInformation("📡 Calling API to fetch customer list from: {RequestUrl}", requestUrl);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var customers = JsonSerializer.Deserialize<List<CustomerModel>>(content, options);

                    _logger.LogInformation("✓ Successfully fetched {Count} customers from API", customers?.Count ?? 0);
                    
                    return Ok(new
                    {
                        Success = true,
                        Data = customers,
                        Message = "Lấy danh sách khách hàng thành công"
                    });
                }

                _logger.LogWarning("✗ API returned status code {StatusCode}", response.StatusCode);
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Response content: {Content}", errorContent);

                return BadRequest(new
                {
                    Success = false,
                    Message = $"Lỗi API: {response.StatusCode}",
                    Details = errorContent
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error loading customers from API");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Lỗi khi tải danh sách khách hàng",
                    Error = ex.Message
                });
            }
        }

        [HttpPost("AddPoints")]
        public async Task<IActionResult> AddPoints([FromBody] AddPointsModel request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.CustomerId) || request.Points <= 0)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "CustomerId và Points phải hợp lệ"
                    });
                }

                // Parse CustomerId string into long to avoid JS precision issues
                if (!long.TryParse(request.CustomerId, out var customerIdLong))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "CustomerId không hợp lệ"
                    });
                }

                var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:44343/";
                var apiKey = _configuration.GetValue<string>("ApiKey");

                if (!apiBaseUrl.EndsWith("/")) apiBaseUrl += "/";

                var requestUrl = $"{apiBaseUrl}api/customer/add-points";
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);

                if (!string.IsNullOrEmpty(apiKey))
                {
                    httpRequest.Headers.Add("X-API-KEY", apiKey);
                }

                // Forward as numeric CustomerId to LoyaltyAPI
                var forwardPayload = new
                {
                    CustomerId = customerIdLong,
                    Points = request.Points,
                    Reason = request.Reason
                };

                var jsonContent = JsonSerializer.Serialize(forwardPayload);
                httpRequest.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation("📡 Calling API to add {Points} points to customer {CustomerId}", request.Points, customerIdLong);

                var response = await _httpClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<AddPointsResponse>(content, options);

                    _logger.LogInformation("✓ Successfully added {Points} points to customer {CustomerId}. New balance: {NewBalance}",
                        request.Points, customerIdLong, result?.NewBalance);

                    return Ok(result);
                }

                _logger.LogWarning("✗ API returned status code {StatusCode}", response.StatusCode);
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Response content: {Content}", errorContent);

                return BadRequest(new AddPointsResponse
                {
                    Success = false,
                    Message = $"Lỗi API: {response.StatusCode}",
                    NewBalance = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error adding points to customer {CustomerId}", request?.CustomerId);
                return StatusCode(500, new AddPointsResponse
                {
                    Success = false,
                    Message = "Lỗi khi cộng điểm",
                    NewBalance = 0
                });
            }
        }
    }
}
