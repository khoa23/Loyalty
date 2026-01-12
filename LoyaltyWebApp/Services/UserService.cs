using LoyaltyWebApp.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LoyaltyWebApp.Services
{
    public class UserService : IUserService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UserService> _logger;

        public UserService(IHttpClientFactory httpClientFactory, ILogger<UserService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var response = await httpClient.PostAsJsonAsync("api/User/login", request);

                _logger.LogInformation("Login API response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Login API response content: {Content}", content);

                    var result = JsonSerializer.Deserialize<JsonElement>(content);

                    if (result.TryGetProperty("data", out var data))
                    {
                        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(data.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        _logger.LogInformation("Login successful for user: {Username}", loginResponse?.Username);
                        return loginResponse;
                    }
                    else
                    {
                        _logger.LogWarning("Login API response does not contain 'Data' property");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Login API failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during login API call");
            }

            return null;
        }

        public async Task<string?> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var response = await httpClient.PostAsJsonAsync("api/User/register", request);

                if (response.IsSuccessStatusCode)
                {
                    return null; // Success
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try 
                    {
                        var errorResult = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorResult.ValueKind == JsonValueKind.Object)
                        {
                            if (errorResult.TryGetProperty("Message", out var msg)) return msg.GetString();
                            if (errorResult.TryGetProperty("message", out var msg2)) return msg2.GetString();
                            if (errorResult.TryGetProperty("error", out var err)) return err.GetString();
                        }
                        // If it's a JSON string, return it
                        if (errorResult.ValueKind == JsonValueKind.String)
                        {
                            return errorResult.GetString();
                        }
                    }
                    catch
                    {
                        // Not valid JSON, use raw content
                    }
                    
                    return string.IsNullOrWhiteSpace(errorContent) ? "Đăng ký thất bại" : errorContent;
                }
            }
            catch (Exception ex)
            {
                return $"Lỗi kết nối: {ex.Message}";
            }
        }

        public async Task<CustomerInfo?> GetCustomerInfoAsync(long userId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var response = await httpClient.GetAsync($"api/User/customer/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var customer = JsonSerializer.Deserialize<CustomerInfo>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return customer;
                }
            }
            catch
            {
                // Return null on error
            }

            return null;
        }

        public async Task<long?> GetCustomerPointsAsync(long userId)
        {
            var customerInfo = await GetCustomerInfoAsync(userId);
            return customerInfo?.Current_Points;
        }
    }
}

