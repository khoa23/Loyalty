using Dapper;
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
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
    }
}
