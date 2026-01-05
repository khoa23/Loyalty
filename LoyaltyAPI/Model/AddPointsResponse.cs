namespace LoyaltyAPI.Model
{
    public class AddPointsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long NewBalance { get; set; }
    }
}
