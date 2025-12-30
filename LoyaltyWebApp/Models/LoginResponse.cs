using System.Text.Json.Serialization;

namespace LoyaltyWebApp.Models
{
    public class LoginResponse
    {
        [JsonPropertyName("user_id")]
        public long User_Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("user_role")]
        public string User_Role { get; set; } = string.Empty;
    }
}

