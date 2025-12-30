using LoyaltyWebApp.Models;
using System.Text.Json;

namespace LoyaltyWebApp.Services
{
    public class RewardService : IRewardService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public RewardService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<List<RewardItem>?> GetRewardsAsync(int page = 1, int pageSize = 100)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var response = await httpClient.GetAsync($"api/Loyalty/rewards?page={page}&pageSize={pageSize}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(content);

                    if (result.TryGetProperty("Data", out var data))
                    {
                        return JsonSerializer.Deserialize<List<RewardItem>>(data.GetRawText(), new JsonSerializerOptions
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

                    if (result.TryGetProperty("Data", out var data))
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

        public async Task<string?> UpdateRewardAsync(string rewardId, string rewardName, string? description, long pointsCost, int stockQuantity, bool isActive, long? lastUpdatedBy)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("LoyaltyAPI");
                var updateRequest = new
                {
                    rewardName = rewardName,
                    description = description ?? "",
                    pointsCost = pointsCost,
                    stockQuantity = stockQuantity,
                    isActive = isActive,
                    lastUpdatedBy = lastUpdatedBy
                };

                var response = await httpClient.PutAsJsonAsync($"api/Loyalty/rewards/{rewardId}", updateRequest);

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

