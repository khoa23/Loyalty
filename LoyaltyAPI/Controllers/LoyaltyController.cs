using Dapper;
using LoyaltyAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;

namespace LoyaltyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoyaltyController : ControllerBase
    {
        private readonly string _connectionString;

        public LoyaltyController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CockroachDb");
        }

        // 1. API Đăng nhập sử dụng function authenticate_user
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);

            // Gọi function bằng SELECT * FROM
            var user = await db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM loyalty_admin.authenticate_user(@username, @pass)",
                new { username = request.Username, pass = request.PasswordHash });

            if (user == null) return Unauthorized("Sai tên đăng nhập hoặc mật khẩu");

            return Ok(new
            {
                Message = "Đăng nhập thành công",
                Data = user
            });
        }

        // 2. API Lấy thông tin khách hàng bằng function get_customer_info
        [HttpGet("customer/{userId}")]
        public async Task<IActionResult> GetCustomer(long userId)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);

            var customer = await db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM loyalty_admin.get_customer_info(@uid)",
                new { uid = userId });

            if (customer == null) return NotFound("Không tìm thấy khách hàng");

            return Ok(customer);
        }

        // 3. API Lấy danh sách quà có sẵn bằng function get_available_rewards_paged
        [HttpGet("rewards")]
        public async Task<IActionResult> GetAvailableRewards([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);

            // Validate parameters
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Giới hạn tối đa 100 items mỗi trang

            // Gọi function get_available_rewards với page và pageSize
            var rewards = await db.QueryAsync<RewardResponse>(
                "SELECT * FROM loyalty_admin.get_available_rewards(@page, @pageSize)",
                new { page = page, pageSize = pageSize });

            var rewardsList = rewards.ToList();

            return Ok(new
            {
                Message = "Lấy danh sách quà thành công",
                Data = rewardsList,
                Page = page,
                PageSize = pageSize,
                TotalCount = rewardsList.Count,
                HasMore = rewardsList.Count == pageSize // Có thể có thêm trang tiếp theo
            });
        }

        // 4. API Thêm quà mới
        [HttpPost("rewards")]
        public async Task<IActionResult> CreateReward([FromBody] CreateRewardRequest request)
        {
            // Validate dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(request.RewardName))
                return BadRequest("Tên quà không được để trống");

            if (request.PointsCost <= 0)
                return BadRequest("Điểm đổi quà phải lớn hơn 0");

            if (request.StockQuantity < 0)
                return BadRequest("Số lượng tồn kho không được âm");

            using IDbConnection db = new NpgsqlConnection(_connectionString);

            try
            {
                // Kiểm tra tên quà đã tồn tại chưa
                var existingReward = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT reward_id FROM loyalty_admin.rewards WHERE reward_name = @rewardName",
                    new { rewardName = request.RewardName });

                if (existingReward != null)
                    return Conflict("Tên quà đã tồn tại");

                // Thêm quà mới
                var rewardId = await db.QuerySingleAsync<int>(
                    @"INSERT INTO loyalty_admin.rewards (reward_name, description, points_cost, stock_quantity, is_active)
                      VALUES (@rewardName, @description, @pointsCost, @stockQuantity, @isActive)
                      RETURNING reward_id",
                    new
                    {
                        rewardName = request.RewardName,
                        description = request.Description ?? string.Empty,
                        pointsCost = request.PointsCost,
                        stockQuantity = request.StockQuantity,
                        isActive = request.IsActive
                    });

                // Lấy thông tin quà vừa tạo
                var newReward = await db.QueryFirstOrDefaultAsync<RewardResponse>(
                    @"SELECT reward_id, reward_name, description, points_cost, stock_quantity, updated_at
                      FROM loyalty_admin.rewards
                      WHERE reward_id = @id",
                    new { id = rewardId });

                return CreatedAtAction(nameof(GetRewardById), new { id = rewardId }, new
                {
                    Message = "Thêm quà thành công",
                    Data = newReward
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi thêm quà: {ex.Message}");
            }
        }

        // 5. API Lấy thông tin quà theo ID
        [HttpGet("rewards/{id}")]
        public async Task<IActionResult> GetRewardById(int id)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);

            var reward = await db.QueryFirstOrDefaultAsync<RewardResponse>(
                @"SELECT reward_id, reward_name, description, points_cost, stock_quantity, updated_at
                  FROM loyalty_admin.rewards
                  WHERE reward_id = @id",
                new { id = id });

            if (reward == null)
                return NotFound("Không tìm thấy quà");

            return Ok(new
            {
                Message = "Lấy thông tin quà thành công",
                Data = reward
            });
        }

        // 6. API Sửa thông tin quà
        [HttpPut("rewards/{id}")]
        public async Task<IActionResult> UpdateReward(int id, [FromBody] UpdateRewardRequest request)
        {
            // Validate dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(request.RewardName))
                return BadRequest("Tên quà không được để trống");

            if (request.PointsCost <= 0)
                return BadRequest("Điểm đổi quà phải lớn hơn 0");

            if (request.StockQuantity < 0)
                return BadRequest("Số lượng tồn kho không được âm");

            using IDbConnection db = new NpgsqlConnection(_connectionString);

            try
            {
                // Kiểm tra quà có tồn tại không
                var existingReward = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT reward_id FROM loyalty_admin.rewards WHERE reward_id = @id",
                    new { id = id });

                if (existingReward == null)
                    return NotFound("Không tìm thấy quà");

                // Kiểm tra tên quà đã tồn tại ở quà khác chưa
                var duplicateName = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT reward_id FROM loyalty_admin.rewards WHERE reward_name = @rewardName AND reward_id != @id",
                    new { rewardName = request.RewardName, id = id });

                if (duplicateName != null)
                    return Conflict("Tên quà đã tồn tại ở quà khác");

                // Cập nhật thông tin quà
                var rowsAffected = await db.ExecuteAsync(
                    @"UPDATE loyalty_admin.rewards
                      SET reward_name = @rewardName,
                          description = @description,
                          points_cost = @pointsCost,
                          stock_quantity = @stockQuantity,
                          is_active = @isActive,
                          updated_at = now()
                      WHERE reward_id = @id",
                    new
                    {
                        id = id,
                        rewardName = request.RewardName,
                        description = request.Description ?? string.Empty,
                        pointsCost = request.PointsCost,
                        stockQuantity = request.StockQuantity,
                        isActive = request.IsActive
                    });

                if (rowsAffected == 0)
                    return NotFound("Không tìm thấy quà để cập nhật");

                // Lấy thông tin quà đã cập nhật
                var updatedReward = await db.QueryFirstOrDefaultAsync<RewardResponse>(
                    @"SELECT reward_id, reward_name, description, points_cost, stock_quantity, updated_at
                      FROM loyalty_admin.rewards
                      WHERE reward_id = @id",
                    new { id = id });

                return Ok(new
                {
                    Message = "Cập nhật quà thành công",
                    Data = updatedReward
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi cập nhật quà: {ex.Message}");
            }
        }

        // 7. API Xóa quà (Soft Delete - set is_active = false)
        [HttpDelete("rewards/{id}")]
        public async Task<IActionResult> DeleteReward(int id)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);

            try
            {
                // Kiểm tra quà có tồn tại không
                var existingReward = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT reward_id FROM loyalty_admin.rewards WHERE reward_id = @id",
                    new { id = id });

                if (existingReward == null)
                    return NotFound("Không tìm thấy quà");

                // Soft delete - set is_active = false thay vì xóa hẳn
                var rowsAffected = await db.ExecuteAsync(
                    @"UPDATE loyalty_admin.rewards
                      SET is_active = false,
                          updated_at = now()
                      WHERE reward_id = @id",
                    new { id = id });

                if (rowsAffected == 0)
                    return NotFound("Không tìm thấy quà để xóa");

                return Ok(new
                {
                    Message = "Xóa quà thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xóa quà: {ex.Message}");
            }
        }
    }

}
