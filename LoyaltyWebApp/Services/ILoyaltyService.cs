using LoyaltyWebApp.Models;

namespace LoyaltyWebApp.Services
{
    public interface ILoyaltyService
    {
        Task<string?> RedeemRewardAsync(long customerId, long rewardId, int quantity);
        Task<List<HistoryItem>?> GetRedemptionHistoryAsync(long customerId, int page = 1, int pageSize = 100);
    }
}

