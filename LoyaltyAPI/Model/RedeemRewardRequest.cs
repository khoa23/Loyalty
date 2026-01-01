using System.Text.Json.Serialization;

namespace LoyaltyAPI.Model
{
    public class RedeemRewardRequest
    {
        [JsonPropertyName("customer_id")]
        public long CustomerId { get; set; }

        [JsonPropertyName("reward_id")]
        public long RewardId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }
}

