using System.Text.Json.Serialization;

namespace LoyaltyWebApp.Models
{
    public class HistoryItem
    {
        [JsonPropertyName("transaction_id")]
        public string Transaction_Id { get; set; } = string.Empty;

        [JsonPropertyName("reward_id")]
        public string Reward_Id { get; set; } = string.Empty;

        [JsonPropertyName("reward_name")]
        public string Reward_Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("quantity_redeemed")]
        public int Quantity_Redeemed { get; set; }

        [JsonPropertyName("points_spent")]
        public long Points_Spent { get; set; }

        [JsonPropertyName("redemption_date")]
        public DateTime Redemption_Date { get; set; }

        [JsonPropertyName("transaction_status")]
        public string Transaction_Status { get; set; } = string.Empty;
    }
}

