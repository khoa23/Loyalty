using LoyaltyWebApp.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LoyaltyWebApp.Services
{
    public class RewardService : IRewardService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RewardService> _logger;

        public RewardService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<RewardService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<RewardItem>?> GetRewardsAsync(int page = 1, int pageSize = 100)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var response = await httpClient.GetAsync($"api/User/customer/rewards?page={page}&pageSize={pageSize}");

                _logger.LogInformation("GetRewards API response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("GetRewards API response content: {Content}", content);

                    var result = JsonSerializer.Deserialize<JsonElement>(content);

                    if (result.TryGetProperty("data", out var data))
                    {
                        var rewards = JsonSerializer.Deserialize<List<RewardItem>>(data.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        _logger.LogInformation("GetRewards successful, count: {Count}", rewards?.Count ?? 0);
                        return rewards;
                    }
                    else
                    {
                        _logger.LogWarning("GetRewards API response does not contain 'Data' property");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("GetRewards API failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during GetRewards API call");
            }

            return null;
        }

        public async Task<PaginatedRewardResult?> GetRewardsWithPaginationAsync(int page = 1, int pageSize = 100)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var response = await httpClient.GetAsync($"api/Loyalty/admin/rewards?page={page}&pageSize={pageSize}");

                _logger.LogInformation("GetRewardsWithPagination API response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("GetRewardsWithPagination API response content: {Content}", content);

                    var result = JsonSerializer.Deserialize<PaginatedRewardResult>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("GetRewardsWithPagination successful, page: {Page}, total: {TotalCount}", result?.Page, result?.TotalCount);
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("GetRewardsWithPagination API failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during GetRewardsWithPagination API call");
            }

            return null;
        }

        public async Task<PaginatedRewardResult?> GetCustomerRewardsWithPaginationAsync(int page = 1, int pageSize = 100)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var response = await httpClient.GetAsync($"api/User/customer/rewards?page={page}&pageSize={pageSize}");

                _logger.LogInformation("GetCustomerRewardsWithPagination API response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("GetCustomerRewardsWithPagination API response content: {Content}", content);

                    var result = JsonSerializer.Deserialize<PaginatedRewardResult>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("GetCustomerRewardsWithPagination successful, page: {Page}, total: {TotalCount}", result?.Page, result?.TotalCount);
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("GetCustomerRewardsWithPagination API failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during GetCustomerRewardsWithPagination API call");
            }

            return null;
        }

        public async Task<RewardItem?> GetRewardByIdAsync(string rewardId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var response = await httpClient.GetAsync($"api/Loyalty/rewards/{rewardId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(content);

                    if (result.TryGetProperty("data", out var data))
                    {
                        return JsonSerializer.Deserialize<RewardItem>(data.GetRawText(), new JsonSerializerOptions
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

        public async Task<string?> CreateRewardAsync(string rewardName, string? description, long pointsCost, int stockQuantity, bool isActive, long? lastUpdatedBy, IFormFile? imageFile)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                using var formData = new MultipartFormDataContent();

                formData.Add(new StringContent(rewardName), "rewardName");
                formData.Add(new StringContent(description ?? ""), "description");
                formData.Add(new StringContent(pointsCost.ToString()), "pointsCost");
                formData.Add(new StringContent(stockQuantity.ToString()), "stockQuantity");
                formData.Add(new StringContent(isActive.ToString().ToLower()), "isActive");
                
                if (lastUpdatedBy.HasValue)
                {
                    formData.Add(new StringContent(lastUpdatedBy.Value.ToString()), "lastUpdatedBy");
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    var streamContent = new StreamContent(imageFile.OpenReadStream());
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
                    formData.Add(streamContent, "imageFile", imageFile.FileName);
                }

                var response = await httpClient.PostAsync("api/Loyalty/rewards", formData);

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
                        : "Thêm quà thất bại";
                }
            }
            catch (Exception ex)
            {
                return $"Lỗi: {ex.Message}";
            }
        }

        public async Task<string?> UpdateRewardAsync(string rewardId, string rewardName, string? description, long pointsCost, int stockQuantity, bool isActive, long? lastUpdatedBy, IFormFile? imageFile)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                using var formData = new MultipartFormDataContent();

                formData.Add(new StringContent(rewardName), "rewardName");
                formData.Add(new StringContent(description ?? ""), "description");
                formData.Add(new StringContent(pointsCost.ToString()), "pointsCost");
                formData.Add(new StringContent(stockQuantity.ToString()), "stockQuantity");
                formData.Add(new StringContent(isActive.ToString().ToLower()), "isActive");

                if (lastUpdatedBy.HasValue)
                {
                    formData.Add(new StringContent(lastUpdatedBy.Value.ToString()), "lastUpdatedBy");
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    var streamContent = new StreamContent(imageFile.OpenReadStream());
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
                    formData.Add(streamContent, "imageFile", imageFile.FileName);
                }

                var response = await httpClient.PutAsync($"api/Loyalty/rewards/{rewardId}", formData);

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
                        : "Cập nhật quà thất bại";
                }
            }
            catch (Exception ex)
            {
                return $"Lỗi: {ex.Message}";
            }
        }

        public async Task<string?> DeleteRewardAsync(string rewardId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var response = await httpClient.DeleteAsync($"api/Loyalty/rewards/{rewardId}");

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
                        : "Xóa quà thất bại";
                }
            }
            catch (Exception ex)
            {
                return $"Lỗi: {ex.Message}";
            }
        }

        public string GetImageUrl(string? imageUrl, string apiBaseUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return "";

            if (imageUrl.StartsWith("http"))
                return imageUrl;

            return apiBaseUrl.TrimEnd('/') + "/" + imageUrl.TrimStart('/');
        }
    }
}

