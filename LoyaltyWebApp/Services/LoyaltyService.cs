using LoyaltyWebApp.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LoyaltyWebApp.Services
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LoyaltyService> _logger;

        public LoyaltyService(IHttpClientFactory httpClientFactory, ILogger<LoyaltyService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string?> RedeemRewardAsync(long customerId, long rewardId, int quantity)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var redeemRequest = new
                {
                    customer_id = customerId,
                    reward_id = rewardId,
                    quantity = quantity
                };

                var response = await httpClient.PostAsJsonAsync("api/Reward/redeem", redeemRequest);

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
                        : "Đổi quà thất bại";
                }
            }
            catch (Exception ex)
            {
                return $"Lỗi: {ex.Message}";
            }
        }

        public async Task<List<HistoryItem>?> GetRedemptionHistoryAsync(long customerId, int page = 1, int pageSize = 100)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var response = await httpClient.GetAsync($"api/Reward/history/{customerId}?page={page}&pageSize={pageSize}");

                _logger.LogInformation("GetRedemptionHistory API response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("GetRedemptionHistory API response content: {Content}", content);

                    var result = JsonSerializer.Deserialize<JsonElement>(content);

                    if (result.TryGetProperty("data", out var data))
                    {
                        var history = JsonSerializer.Deserialize<List<HistoryItem>>(data.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        _logger.LogInformation("GetRedemptionHistory successful for customer {CustomerId}, page {Page}, pageSize {PageSize}, count {Count}", customerId, page, pageSize, history?.Count ?? 0);
                        return history;
                    }
                    else
                    {
                        _logger.LogWarning("GetRedemptionHistory API response does not contain 'Data' property");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("GetRedemptionHistory API failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during GetRedemptionHistory API call for customer {CustomerId}, page {Page}, pageSize {PageSize}", customerId, page, pageSize);
            }

            return null;
        }
    }
}

