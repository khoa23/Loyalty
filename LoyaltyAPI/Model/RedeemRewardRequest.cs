namespace LoyaltyAPI.Model
{
    public class RedeemRewardRequest
    {
        public long CustomerId { get; set; }
        public long RewardId { get; set; }
        public int Quantity { get; set; }
    }
}

