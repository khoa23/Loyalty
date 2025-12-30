using LoyaltyWebApp.Models;
using System.Text.Json;

namespace LoyaltyWebApp.Services
{
    public class UserService : IUserService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UserService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var response = await httpClient.PostAsJsonAsync("api/User/login", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(content);

                    if (result.TryGetProperty("data", out var data))
                    {
                        return JsonSerializer.Deserialize<LoginResponse>(
                            data.GetRawText(),
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                    }

                }
            }
            catch
            {
                // Return null on error
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
                    var errorResult = JsonSerializer.Deserialize<JsonElement>(errorContent);
                    return errorResult.TryGetProperty("Message", out var msg) 
                        ? msg.GetString() 
                        : "Đăng ký thất bại";
                }
            }
            catch (Exception ex)
            {
                return $"Lỗi kết nối: {ex.Message}";
            }
        }

        public async Task<long?> GetCustomerPointsAsync(long userId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var response = await httpClient.GetAsync($"api/User/customer/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var customer = JsonSerializer.Deserialize<JsonElement>(content);

                    if (customer.TryGetProperty("current_points", out var points))
                    {
                        return points.GetInt64();
                    }
                }
            }
            catch
            {
                // Return null on error
            }

            return null;
        }
    }
}

