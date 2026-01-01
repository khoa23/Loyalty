using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LoyaltyWebApp.Models
{
    public class RewardViewModel
    {
        [JsonPropertyName("reward_Id")]
        public string Reward_Id { get; set; }

        [JsonPropertyName("reward_Name")]
        public string Reward_Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("points_Cost")]
        public int Points_Cost { get; set; }

        [JsonPropertyName("stock_Quantity")]
        public int Stock_Quantity { get; set; }

        [JsonPropertyName("image_Url")]
        public string Image_Url { get; set; }
    }

    public class RewardListViewModel
    {
        [JsonPropertyName("data")]
        public List<RewardViewModel> Data { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("hasPrevious")]
        public bool HasPrevious { get; set; }

        [JsonPropertyName("hasNext")]
        public bool HasNext { get; set; }
    }
}