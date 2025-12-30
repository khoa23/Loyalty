namespace LoyaltyAPI.Model
{
    public class RewardResponse
    {
        public string Reward_Id { get; set; }
        public string Reward_Name { get; set; }
        public string Description { get; set; }
        public long Points_Cost { get; set; }
        public int Stock_Quantity { get; set; }
        public string? Image_Url { get; set; }
        public DateTime Updated_At { get; set; }
    }
}
