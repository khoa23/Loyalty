using System.Text.Json.Serialization;

namespace LoyaltyWebApp.Models
{
    public class PaginatedRewardResult
    {
        [JsonPropertyName("data")]
        public List<RewardItem> Data { get; set; } = new();

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("hasNext")]
        public bool HasNext { get; set; }

        [JsonPropertyName("hasPrevious")]
        public bool HasPrevious { get; set; }
    }
}