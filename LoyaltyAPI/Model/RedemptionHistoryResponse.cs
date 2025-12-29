namespace LoyaltyAPI.Model
{
    public class RedemptionHistoryResponse
    {
        public string Transaction_Id { get; set; }
        public string Reward_Id { get; set; }
        public string Reward_Name { get; set; }
        public string Description { get; set; }
        public int Quantity_Redeemed { get; set; }
        public long Points_Spent { get; set; }
        public DateTime Redemption_Date { get; set; }
        public string Transaction_Status { get; set; }
    }
}

