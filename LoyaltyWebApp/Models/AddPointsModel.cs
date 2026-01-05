namespace LoyaltyWebApp.Models
{
    public class AddPointsModel
    {
        public string CustomerId { get; set; }
        public long Points { get; set; }
        public string Reason { get; set; }
    }
}
