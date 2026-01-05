namespace LoyaltyWebApp.Models
{
    public class AddPointsModel
    {
        public int CustomerId { get; set; }
        public long Points { get; set; }
        public string Reason { get; set; }
    }
}
