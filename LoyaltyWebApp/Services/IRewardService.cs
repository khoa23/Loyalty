using LoyaltyWebApp.Models;

namespace LoyaltyWebApp.Services
{
    public interface IRewardService
    {
        Task<List<RewardItem>?> GetRewardsAsync(int page = 1, int pageSize = 100);
        Task<RewardItem?> GetRewardByIdAsync(string rewardId);
        Task<string?> CreateRewardAsync(string rewardName, string? description, long pointsCost, int stockQuantity, bool isActive, long? lastUpdatedBy, IFormFile? imageFile);
        Task<string?> UpdateRewardAsync(string rewardId, string rewardName, string? description, long pointsCost, int stockQuantity, bool isActive, long? lastUpdatedBy);
        Task<string?> DeleteRewardAsync(string rewardId);
        string GetImageUrl(string? imageUrl, string apiBaseUrl);
    }
}

