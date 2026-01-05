using Dapper;
using LoyaltyAPI.Helpers;
using LoyaltyAPI.Model;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace LoyaltyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(IConfiguration configuration, ILogger<CustomerController> logger)
        {
            _connectionString = configuration.GetConnectionString("CockroachDb");
            _logger = logger;
        }

        // API Lấy danh sách tất cả khách hàng
        [HttpGet("list")]
        public async Task<IActionResult> ListCustomers()
        {
            try
            {
                using IDbConnection db = DatabaseConnectionHelper.CreateConnection(_connectionString, _logger);

                var customers = await db.QueryAsync<CustomerResponse>(
                    @"SELECT 
                        c.customer_id,
                        c.user_id,
                        c.cif_number,
                        c.full_name,
                        u.username,
                        c.current_points,
                        c.phone_number,
                        c.updated_at
                    FROM loyalty_admin.customers c
                    INNER JOIN loyalty_admin.users u ON c.user_id = u.user_id
                    ORDER BY c.updated_at DESC");

                _logger.LogInformation("Retrieved {Count} customers", customers.Count());
                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer list");
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách khách hàng", error = ex.Message });
            }
        }

        // API Thêm điểm cho khách hàng
        [HttpPost("add-points")]
        public async Task<IActionResult> AddPoints([FromBody] AddPointsRequest request)
        {
            if (request == null || request.CustomerId <= 0 || request.Points <= 0)
            {
                return BadRequest("CustomerId và Points phải hợp lệ");
            }

            try
            {
                using IDbConnection db = DatabaseConnectionHelper.CreateConnection(_connectionString, _logger);

                // Kiểm tra khách hàng tồn tại
                var customer = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM loyalty_admin.customers WHERE customer_id = @customerId",
                    new { customerId = request.CustomerId });

                if (customer == null)
                {
                    return NotFound("Khách hàng không tồn tại");
                }

                // Cập nhật điểm
                var newBalance = (long)customer.current_points + request.Points;
                
                await db.ExecuteAsync(
                    @"UPDATE loyalty_admin.customers 
                      SET current_points = @newBalance, 
                          updated_at = now() 
                      WHERE customer_id = @customerId",
                    new { newBalance, customerId = request.CustomerId });

                _logger.LogInformation("Added {Points} points to customer {CustomerId}. New balance: {NewBalance}. Reason: {Reason}",
                    request.Points, request.CustomerId, newBalance, request.Reason);

                return Ok(new AddPointsResponse
                {
                    Success = true,
                    Message = "Cộng điểm thành công",
                    NewBalance = newBalance
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding points to customer {CustomerId}", request.CustomerId);
                return StatusCode(500, new AddPointsResponse
                {
                    Success = false,
                    Message = "Lỗi khi cộng điểm",
                    NewBalance = 0
                });
            }
        }

        // API Lấy thông tin chi tiết khách hàng
        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetCustomer(int customerId)
        {
            try
            {
                using IDbConnection db = DatabaseConnectionHelper.CreateConnection(_connectionString, _logger);

                var customer = await db.QueryFirstOrDefaultAsync<CustomerResponse>(
                    @"SELECT 
                        c.customer_id,
                        c.user_id,
                        c.cif_number,
                        c.full_name,
                        u.username,
                        c.current_points,
                        c.phone_number,
                        c.updated_at
                    FROM loyalty_admin.customers c
                    INNER JOIN loyalty_admin.users u ON c.user_id = u.user_id
                    WHERE c.customer_id = @customerId",
                    new { customerId });

                if (customer == null)
                {
                    return NotFound("Khách hàng không tồn tại");
                }

                return Ok(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer {CustomerId}", customerId);
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin khách hàng", error = ex.Message });
            }
        }
    }
}
