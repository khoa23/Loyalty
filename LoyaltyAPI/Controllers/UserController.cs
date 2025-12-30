using Dapper;
using LoyaltyAPI.Model;
using LoyaltyAPI.Security;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;

namespace LoyaltyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly string _connectionString;

        public UserController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CockroachDb");
        }

        // API Đăng nhập với băm password bằng BCrypt
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Tên đăng nhập và mật khẩu không được để trống");
            }

            using IDbConnection db = new NpgsqlConnection(_connectionString);

            // Lấy thông tin user từ database (bao gồm password_hash)
            var user = await db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM loyalty_admin.get_user_by_username(@username)",
                new { username = request.Username });

            if (user == null)
            {
                return Unauthorized("Sai tên đăng nhập hoặc mật khẩu");
            }

            // Verify password bằng BCrypt
            string storedPasswordHash = user.password_hash;
            bool isPasswordValid = PasswordHasher.VerifyPassword(request.Password, storedPasswordHash);

            if (!isPasswordValid)
            {
                return Unauthorized("Sai tên đăng nhập hoặc mật khẩu");
            }

            // Trả về thông tin user (không bao gồm password_hash)
            return Ok(new
            {
                Message = "Đăng nhập thành công",
                Data = new
                {
                    user_id = user.user_id,
                    username = user.username,
                    user_role = user.user_role
                }
            });
        }

        // API Đăng ký user mới
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest("Tên đăng nhập không được để trống");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Mật khẩu không được để trống");
            }

            // Validate độ dài password (tối thiểu 6 ký tự)
            if (request.Password.Length < 6)
            {
                return BadRequest("Mật khẩu phải có ít nhất 6 ký tự");
            }

            // Validate độ dài username (tối thiểu 3 ký tự, tối đa 50)
            if (request.Username.Length < 3 || request.Username.Length > 50)
            {
                return BadRequest("Tên đăng nhập phải có từ 3 đến 50 ký tự");
            }

            // userRole luôn là "Customer"
            string userRole = "Customer";

            // full_name là bắt buộc
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest("Họ và tên không được để trống");
            }

            // Validate độ dài full_name (nếu có)
            if (!string.IsNullOrWhiteSpace(request.FullName) && request.FullName.Length > 100)
            {
                return BadRequest("Họ và tên không được vượt quá 100 ký tự");
            }

            // Validate độ dài phone_number (nếu có)
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && request.PhoneNumber.Length > 15)
            {
                return BadRequest("Số điện thoại không được vượt quá 15 ký tự");
            }

            using IDbConnection db = new NpgsqlConnection(_connectionString);

            // Kiểm tra username đã tồn tại chưa
            var existingUser = await db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM loyalty_admin.get_user_by_username(@username)",
                new { username = request.Username });

            if (existingUser != null)
            {
                return Conflict("Tên đăng nhập đã tồn tại");
            }

            // Hash password bằng BCrypt
            string passwordHash = PasswordHasher.HashPassword(request.Password);

            // Sử dụng transaction để đảm bảo atomicity
            db.Open();
            using var transaction = db.BeginTransaction();

            try
            {
                // Insert user mới vào database
                var newUserId = await db.QuerySingleAsync<long>(
                    @"INSERT INTO loyalty_admin.users (username, password_hash, user_role) 
                      VALUES (@username, @passwordHash, @userRole) 
                      RETURNING user_id",
                    new
                    {
                        username = request.Username,
                        passwordHash = passwordHash,
                        userRole = userRole
                    },
                    transaction);

                // Insert vào bảng customers (luôn là Customer)
                // Tạo CIF number tự động
                string cifNumber = await GenerateCifNumberAsync(db, transaction);

                // Insert customer
                await db.ExecuteAsync(
                    @"INSERT INTO loyalty_admin.customers (user_id, cif_number, full_name, current_points, phone_number) 
                      VALUES (@userId, @cifNumber, @fullName, 0, @phoneNumber)",
                    new
                    {
                        userId = newUserId,
                        cifNumber = cifNumber,
                        fullName = request.FullName,
                        phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? (string?)null : request.PhoneNumber
                    },
                    transaction);

                // Commit transaction
                transaction.Commit();

                // Lấy thông tin user vừa tạo
                var newUser = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM loyalty_admin.get_user_by_username(@username)",
                    new { username = request.Username });

                return CreatedAtAction(
                    nameof(Login),
                    new { username = request.Username },
                    new
                    {
                        Message = "Đăng ký thành công",
                        Data = new
                        {
                            user_id = newUser.user_id,
                            username = newUser.username,
                            user_role = newUser.user_role,
                            created_at = newUser.created_at
                        }
                    });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505") // Unique violation
            {
                transaction.Rollback();
                return Conflict("Tên đăng nhập đã tồn tại");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, $"Lỗi khi đăng ký: {ex.Message}");
            }
        }

        // Helper method để tạo CIF number tự động
        private async Task<string> GenerateCifNumberAsync(IDbConnection db, IDbTransaction transaction)
        {
            // Lấy số lớn nhất từ CIF number hiện có
            var maxCifNumber = await db.QueryFirstOrDefaultAsync<string>(
                @"SELECT MAX(cif_number) 
                  FROM loyalty_admin.customers 
                  WHERE cif_number LIKE 'CIF%'",
                transaction: transaction);

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(maxCifNumber) && maxCifNumber.Length > 3)
            {
                // Extract số từ CIF (ví dụ: CIF000001 -> 1)
                string numberPart = maxCifNumber.Substring(3); // Bỏ qua "CIF"
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            // Format CIF number với 6 chữ số (ví dụ: CIF000001)
            return $"CIF{nextNumber:D6}";
        }

        // API Lấy thông tin khách hàng bằng function get_customer_info
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
}
