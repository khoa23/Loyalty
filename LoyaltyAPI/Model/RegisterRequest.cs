namespace LoyaltyAPI.Model
{
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; } // Tên đầy đủ (bắt buộc)
        public string? PhoneNumber { get; set; } // Số điện thoại (tùy chọn)
    }
}

