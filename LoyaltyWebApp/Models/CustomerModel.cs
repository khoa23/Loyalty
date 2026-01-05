namespace LoyaltyWebApp.Models
{
    public class CustomerModel
    {
        public long CustomerId { get; set; }
        public long UserId { get; set; }
        public string CifNumber { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public long CurrentPoints { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
