using Dapper;
using LoyaltyAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace LoyaltyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RewardController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<RewardController> _logger;

        public RewardController(IConfiguration configuration, ILogger<RewardController> logger)
        {
            _connectionString = configuration.GetConnectionString("CockroachDb");
            _logger = logger;
        }

        // API Đổi quà
        [HttpPost("redeem")]
        public async Task<IActionResult> RedeemReward([FromBody] RedeemRewardRequest request)
        {
            // Validate dữ liệu đầu vào
            if (request.CustomerId <= 0)
                return BadRequest("Customer ID không hợp lệ");

            if (request.RewardId <= 0)
                return BadRequest("Reward ID không hợp lệ");

            if (request.Quantity <= 0)
                return BadRequest("Số lượng phải lớn hơn 0");

            _logger.LogInformation("Redeem reward request: CustomerId={CustomerId}, RewardId={RewardId}, Quantity={Quantity}", request.CustomerId, request.RewardId, request.Quantity);

            using IDbConnection db = new NpgsqlConnection(_connectionString);

            try
            {
                // Gọi function redeem_reward
                var result = await db.QueryFirstOrDefaultAsync<RedeemRewardResponse>(
                    "SELECT * FROM loyalty_admin.redeem_reward(@customerId, @rewardId, @quantity)",
                    new
                    {
                        customerId = request.CustomerId,
                        rewardId = request.RewardId,
                        quantity = request.Quantity
                    });

                if (result == null)
                {
                    _logger.LogWarning("Failed to redeem reward for customer {CustomerId}, reward {RewardId}, quantity {Quantity}: redeem function returned null", request.CustomerId, request.RewardId, request.Quantity);
                    return BadRequest("Không thể đổi quà. Vui lòng kiểm tra lại thông tin.");
                }

                _logger.LogInformation("Reward redeemed successfully for customer {CustomerId}, reward {RewardId}, quantity {Quantity}", request.CustomerId, request.RewardId, request.Quantity);
                return Ok(new
                {
                    Message = "Đổi quà thành công",
                    Data = result
                });
            }
            catch (PostgresException ex)
            {
                // Xử lý lỗi từ PostgreSQL function (RAISE EXCEPTION)
                _logger.LogWarning(ex, "PostgreSQL exception during reward redemption for customer {CustomerId}, reward {RewardId}, quantity {Quantity}", request.CustomerId, request.RewardId, request.Quantity);
                return BadRequest($"Lỗi khi đổi quà: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during reward redemption for customer {CustomerId}, reward {RewardId}, quantity {Quantity}", request.CustomerId, request.RewardId, request.Quantity);
                return StatusCode(500, $"Lỗi khi đổi quà: {ex.Message}");
            }
        }

        // API Lấy lịch sử đổi quà của customer
        [HttpGet("history/{customerId}")]
        public async Task<IActionResult> GetRedemptionHistory(
            long customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validate parameters
            if (customerId <= 0)
                return BadRequest("Customer ID không hợp lệ");

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            using IDbConnection db = new NpgsqlConnection(_connectionString);

            try
            {
                // Gọi function get_redemption_history
                var history = await db.QueryAsync<RedemptionHistoryResponse>(
                    "SELECT * FROM loyalty_admin.get_redemption_history(@customerId, @page, @pageSize)",
                    new
                    {
                        customerId = customerId,
                        page = page,
                        pageSize = pageSize
                    });

                var historyList = history.ToList();

                _logger.LogInformation("Redemption history retrieved successfully for customer {CustomerId}, page {Page}, pageSize {PageSize}, count {Count}", customerId, page, pageSize, historyList.Count);
                return Ok(new
                {
                    Message = "Lấy lịch sử đổi quà thành công",
                    Data = historyList,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = historyList.Count,
                    HasMore = historyList.Count == pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during redemption history retrieval for customer {CustomerId}, page {Page}, pageSize {PageSize}", customerId, page, pageSize);
                return StatusCode(500, $"Lỗi khi lấy lịch sử đổi quà: {ex.Message}");
            }
        }
    }
}
