namespace LoyaltyAPI.Model
{
    public class UpdateRewardRequest
    {
        public string RewardName { get; set; }
        public string Description { get; set; }
        public long PointsCost { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public long? LastUpdatedBy { get; set; }
    }
}

