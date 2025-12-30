using System.Text.Json.Serialization;

namespace LoyaltyWebApp.Models
{
    public class RewardItem
    {
        [JsonPropertyName("reward_id")]
        public string Reward_Id { get; set; } = string.Empty;

        [JsonPropertyName("reward_name")]
        public string Reward_Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("points_cost")]
        public long Points_Cost { get; set; }

        [JsonPropertyName("stock_quantity")]
        public int Stock_Quantity { get; set; }

        [JsonPropertyName("image_url")]
        public string? Image_Url { get; set; }

        [JsonPropertyName("is_active")]
        public bool? Is_Active { get; set; }
    }
}

