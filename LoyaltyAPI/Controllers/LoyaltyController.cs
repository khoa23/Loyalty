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
        private readonly IConfiguration _configuration;

        public LoyaltyController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CockroachDb");
            _configuration = configuration;
        }

        // 1. API Lấy danh sách quà có sẵn bằng function get_available_rewards_paged
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

        // 4. API Thêm quà mới (có thể upload ảnh cùng lúc)
        [HttpPost("rewards")]
        public async Task<IActionResult> CreateReward(
            [FromForm] string rewardName,
            [FromForm] string description,
            [FromForm] long pointsCost,
            [FromForm] int stockQuantity,
            [FromForm] bool isActive = true,
            IFormFile? imageFile = null)
        {
            // Validate dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(rewardName))
                return BadRequest("Tên quà không được để trống");

            if (pointsCost <= 0)
                return BadRequest("Điểm đổi quà phải lớn hơn 0");

            if (stockQuantity < 0)
                return BadRequest("Số lượng tồn kho không được âm");

            // Lấy lastUpdatedBy trực tiếp từ Request.Form để đảm bảo nhận giá trị string gốc chính xác
            // Tránh bị làm tròn khi model binding tự động convert
            string? lastUpdatedByRaw = Request.Form["lastUpdatedBy"].FirstOrDefault();
            
            // Parse lastUpdatedBy từ string sang long? để tránh mất precision với số lớn
            long? lastUpdatedByValue = null;
            string? lastUpdatedByString = null;
            if (!string.IsNullOrWhiteSpace(lastUpdatedByRaw))
            {
                // Lưu giá trị string gốc để so sánh (lấy trực tiếp từ Request.Form)
                lastUpdatedByString = lastUpdatedByRaw.Trim();
                
                // Loại bỏ các ký tự không phải số (trừ dấu trừ ở đầu)
                var cleanString = lastUpdatedByString;
                if (cleanString.StartsWith("-"))
                {
                    cleanString = "-" + string.Join("", cleanString.Substring(1).Where(char.IsDigit));
                }
                else
                {
                    cleanString = string.Join("", cleanString.Where(char.IsDigit));
                }
                
                // Kiểm tra độ dài - số lớn hơn 15 chữ số có thể bị làm tròn trong JavaScript
                if (cleanString.Length > 15)
                {
                    // Với số lớn, chỉ chấp nhận nếu string gốc giống hệt sau khi parse
                    if (!long.TryParse(cleanString, out long parsedValue))
                    {
                        return BadRequest($"lastUpdatedBy phải là một số hợp lệ trong phạm vi long. Giá trị nhận được: '{lastUpdatedByString}'");
                    }
                    
                    // Kiểm tra chính xác: string sau khi parse phải giống hệt string gốc
                    var parsedString = parsedValue.ToString();
                    if (cleanString != parsedString)
                    {
                        return BadRequest($"lastUpdatedBy bị mất precision do làm tròn. " +
                                        $"Giá trị gốc: '{lastUpdatedByString}' → '{cleanString}', " +
                                        $"Giá trị sau parse: '{parsedString}'. " +
                                        $"Vui lòng đảm bảo gửi dưới dạng STRING trong form-data: " +
                                        $"lastUpdatedBy='{lastUpdatedByString}' (có dấu nháy đơn) " +
                                        $"hoặc trong code JavaScript: formData.append('lastUpdatedBy', '{lastUpdatedByString}')");
                    }
                    
                    lastUpdatedByValue = parsedValue;
                }
                else
                {
                    // Với số nhỏ hơn, parse bình thường
                    if (!long.TryParse(cleanString, out long parsedValue))
                    {
                        return BadRequest($"lastUpdatedBy phải là một số hợp lệ. Giá trị nhận được: '{lastUpdatedByString}'");
                    }
                    lastUpdatedByValue = parsedValue;
                }
            }

            // Lấy cấu hình upload nếu có file ảnh
            string? imageUrl = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                // Validate file size
                var maxFileSize = _configuration.GetValue<long>("UploadConfig:MaxFileSize", 5 * 1024 * 1024);
                if (imageFile.Length > maxFileSize)
                {
                    var maxSizeMB = maxFileSize / (1024 * 1024);
                    return BadRequest($"Kích thước file không được vượt quá {maxSizeMB}MB");
                }

                // Validate file extension
                var allowedExtensions = _configuration.GetSection("UploadConfig:AllowedExtensions").Get<string[]>()
                    ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    var extensionsList = string.Join(", ", allowedExtensions);
                    return BadRequest($"Chỉ chấp nhận file ảnh với định dạng: {extensionsList}");
                }
            }

            using IDbConnection db = new NpgsqlConnection(_connectionString);

            try
            {
                // Kiểm tra tên quà đã tồn tại chưa
                var existingReward = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT reward_id FROM loyalty_admin.rewards WHERE reward_name = @rewardName",
                    new { rewardName = rewardName });

                if (existingReward != null)
                    return Conflict("Tên quà đã tồn tại");

                // Kiểm tra lastUpdatedBy có tồn tại trong bảng users không (nếu có giá trị)
                if (lastUpdatedByValue.HasValue && !string.IsNullOrWhiteSpace(lastUpdatedByString))
                {
                    // Thử tìm user với string chính xác trước (tránh làm tròn)
                    var existingUserByString = await db.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT user_id FROM loyalty_admin.users WHERE user_id::text = @userIdString",
                        new { userIdString = lastUpdatedByString });

                    if (existingUserByString != null)
                    {
                        // Tìm thấy bằng string, sử dụng giá trị chính xác từ database
                        lastUpdatedByValue = (long)existingUserByString.user_id;
                    }
                    else
                    {
                        // Nếu không tìm thấy bằng string, thử tìm bằng long (có thể giá trị đã bị làm tròn)
                        var existingUser = await db.QueryFirstOrDefaultAsync<dynamic>(
                            "SELECT user_id FROM loyalty_admin.users WHERE user_id = @userId",
                            new { userId = lastUpdatedByValue.Value });

                        if (existingUser == null)
                        {
                            // Nếu vẫn không tìm thấy, thử tìm user có ID gần nhất
                            // Tìm user có ID trong khoảng ±10000 từ giá trị đã nhận
                            var nearbyUser = await db.QueryFirstOrDefaultAsync<dynamic>(
                                @"SELECT user_id FROM loyalty_admin.users 
                                  WHERE user_id BETWEEN @minId AND @maxId
                                  ORDER BY ABS(user_id - @userId) 
                                  LIMIT 1",
                                new 
                                { 
                                    minId = lastUpdatedByValue.Value - 10000,
                                    maxId = lastUpdatedByValue.Value + 10000,
                                    userId = lastUpdatedByValue.Value 
                                });

                            if (nearbyUser != null)
                            {
                                // Tìm thấy user gần đó - tự động sửa nếu chênh lệch nhỏ (do làm tròn)
                                var foundUserId = (long)nearbyUser.user_id;
                                var difference = Math.Abs(foundUserId - lastUpdatedByValue.Value);
                                
                                // Nếu chênh lệch nhỏ hơn 1000, có thể do làm tròn, tự động sửa
                                if (difference < 1000)
                                {
                                    // Tự động sửa giá trị và tiếp tục
                                    lastUpdatedByValue = foundUserId;
                                    // Log hoặc có thể trả về warning nhưng vẫn cho phép tiếp tục
                                }
                                else
                                {
                                    return BadRequest($"Không tìm thấy user với ID: {lastUpdatedByString}. " +
                                                    $"Tìm thấy user gần nhất với ID: {foundUserId} (chênh lệch: {difference}). " +
                                                    $"Giá trị bạn gửi có thể đã bị làm tròn. " +
                                                    $"Vui lòng sử dụng ID chính xác: {foundUserId} " +
                                                    $"hoặc đảm bảo gửi giá trị dưới dạng string trong form-data: lastUpdatedBy='{foundUserId}'");
                                }
                            }

                            // Lấy danh sách một vài user_id để người dùng tham khảo
                            var sampleUsers = await db.QueryAsync<dynamic>(
                                "SELECT user_id FROM loyalty_admin.users ORDER BY user_id DESC LIMIT 5");
                            
                            var sampleUserIds = string.Join(", ", sampleUsers.Select(u => u.user_id.ToString()));
                            
                            return BadRequest($"Không tìm thấy user với ID: {lastUpdatedByString}. " +
                                            $"Giá trị đã nhận: '{lastUpdatedByString}' (có thể đã bị làm tròn từ giá trị gốc). " +
                                            $"Một số user_id có sẵn: {sampleUserIds}. " +
                                            $"Vui lòng kiểm tra lại lastUpdatedBy và đảm bảo gửi dưới dạng string trong form-data: " +
                                            $"lastUpdatedBy='1134039809002635265' (có dấu nháy đơn).");
                        }
                    }
                }

                // Thêm quà mới
                var rewardId = await db.QuerySingleAsync<long>(
                    @"INSERT INTO loyalty_admin.rewards (reward_name, description, points_cost, stock_quantity, is_active, last_updated_by, image_url)
                      VALUES (@rewardName, @description, @pointsCost, @stockQuantity, @isActive, @lastUpdatedBy, @imageUrl)
                      RETURNING reward_id",
                    new
                    {
                        rewardName = rewardName,
                        description = description ?? string.Empty,
                        pointsCost = pointsCost,
                        stockQuantity = stockQuantity,
                        isActive = isActive,
                        lastUpdatedBy = lastUpdatedByValue,
                        imageUrl = (string?)null // Sẽ cập nhật sau khi upload file
                    });

                // Upload ảnh nếu có
                if (imageFile != null && imageFile.Length > 0)
                {
                    var rewardImageFolder = _configuration.GetValue<string>("UploadConfig:RewardImageFolder", "Uploads/rewards");
                    var rewardImageUrlPrefix = _configuration.GetValue<string>("UploadConfig:RewardImageUrlPrefix", "/Uploads/rewards");
                    var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                    // Tạo thư mục upload nếu chưa tồn tại
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), rewardImageFolder);
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Tạo tên file unique (reward_id + timestamp + extension)
                    var fileName = $"reward_{rewardId}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    // Lưu file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Tạo đường dẫn URL (relative path)
                    imageUrl = $"{rewardImageUrlPrefix}/{fileName}";

                    // Cập nhật image_url vào database
                    await db.ExecuteAsync(
                        @"UPDATE loyalty_admin.rewards 
                          SET image_url = @imageUrl, updated_at = now()
                          WHERE reward_id = @id",
                        new { id = rewardId, imageUrl = imageUrl });
                }

                // Lấy thông tin quà vừa tạo
                var newReward = await db.QueryFirstOrDefaultAsync<RewardResponse>(
                    @"SELECT reward_id::text as reward_id, reward_name, description, points_cost, stock_quantity, image_url, updated_at
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
        public async Task<IActionResult> GetRewardById(long id)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);

            var reward = await db.QueryFirstOrDefaultAsync<RewardResponse>(
                @"SELECT reward_id::text as reward_id, reward_name, description, points_cost, stock_quantity, image_url, updated_at
                  FROM loyalty_admin.rewards
                  WHERE reward_id = @id::INT",
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
        public async Task<IActionResult> UpdateReward(long id, [FromBody] UpdateRewardRequest request)
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
                // Kiểm tra quà có tồn tại không - sử dụng CAST để đảm bảo type matching
                var existingReward = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT reward_id FROM loyalty_admin.rewards WHERE reward_id = @id::INT",
                    new { id = id });

                if (existingReward == null)
                    return NotFound($"Không tìm thấy quà với ID: {id}");

                // Kiểm tra tên quà đã tồn tại ở quà khác chưa
                var duplicateName = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT reward_id FROM loyalty_admin.rewards WHERE reward_name = @rewardName AND reward_id != @id::INT",
                    new { rewardName = request.RewardName, id = id });

                if (duplicateName != null)
                    return Conflict("Tên quà đã tồn tại ở quà khác");

                // Cập nhật thông tin quà - sử dụng CAST để đảm bảo type matching
                var rowsAffected = await db.ExecuteAsync(
                    @"UPDATE loyalty_admin.rewards
                      SET reward_name = @rewardName,
                          description = @description,
                          points_cost = @pointsCost,
                          stock_quantity = @stockQuantity,
                          is_active = @isActive,
                          last_updated_by = @lastUpdatedBy,
                          updated_at = now()
                      WHERE reward_id = @id::INT",
                    new
                    {
                        id = id,
                        rewardName = request.RewardName,
                        description = request.Description ?? string.Empty,
                        pointsCost = request.PointsCost,
                        stockQuantity = request.StockQuantity,
                        isActive = request.IsActive,
                        lastUpdatedBy = request.LastUpdatedBy.HasValue ? (long?)request.LastUpdatedBy.Value : null
                    });

                if (rowsAffected == 0)
                    return NotFound($"Không tìm thấy quà với ID: {id} để cập nhật");

                // Lấy thông tin quà đã cập nhật - sử dụng CAST để đảm bảo type matching
                var updatedReward = await db.QueryFirstOrDefaultAsync<RewardResponse>(
                    @"SELECT reward_id::text as reward_id, reward_name, description, points_cost, stock_quantity, image_url, updated_at
                      FROM loyalty_admin.rewards
                      WHERE reward_id = @id::INT",
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
        public async Task<IActionResult> DeleteReward(long id)
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);

            try
            {
                // Kiểm tra quà có tồn tại không - sử dụng CAST để đảm bảo type matching
                var existingReward = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT reward_id FROM loyalty_admin.rewards WHERE reward_id = @id::INT",
                    new { id = id });

                if (existingReward == null)
                    return NotFound($"Không tìm thấy quà với ID: {id}");

                // Soft delete - set is_active = false thay vì xóa hẳn
                var rowsAffected = await db.ExecuteAsync(
                    @"UPDATE loyalty_admin.rewards
                      SET is_active = false,
                          updated_at = now()
                      WHERE reward_id = @id::INT",
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
