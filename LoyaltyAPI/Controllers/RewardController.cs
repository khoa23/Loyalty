using Dapper;
using LoyaltyAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;

namespace LoyaltyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RewardController : ControllerBase
    {
        private readonly string _connectionString;

        public RewardController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CockroachDb");
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
                    return BadRequest("Không thể đổi quà. Vui lòng kiểm tra lại thông tin.");

                return Ok(new
                {
                    Message = "Đổi quà thành công",
                    Data = result
                });
            }
            catch (PostgresException ex)
            {
                // Xử lý lỗi từ PostgreSQL function (RAISE EXCEPTION)
                return BadRequest($"Lỗi khi đổi quà: {ex.Message}");
            }
            catch (Exception ex)
            {
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
                return StatusCode(500, $"Lỗi khi lấy lịch sử đổi quà: {ex.Message}");
            }
        }
    }
}
