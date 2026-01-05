namespace LoyaltyAPI.Model
{
    public class AddPointsRequest
    {
        public long CustomerId { get; set; }
        public long Points { get; set; }
        public string Reason { get; set; }
    }
}
