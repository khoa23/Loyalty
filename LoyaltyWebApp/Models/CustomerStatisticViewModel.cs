namespace LoyaltyWebApp.Models
{
    public class CustomerStatisticViewModel
    {
        public long CustomerId { get; set; }
        public string FullName { get; set; }
        public string CifNumber { get; set; }
        public int TotalGiftsRedeemed { get; set; }
        public long TotalPointsSpent { get; set; }
    }
}
