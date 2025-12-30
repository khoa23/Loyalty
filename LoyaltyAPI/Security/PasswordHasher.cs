using BCrypt.Net;

namespace LoyaltyAPI.Security
{
    /// <summary>
    /// Helper class để hash và verify password sử dụng BCrypt
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Hash password với BCrypt (tự động tạo salt)
        /// </summary>
        /// <param name="password">Password plain text cần hash</param>
        /// <param name="workFactor">Độ phức tạp (mặc định 12 - cân bằng giữa bảo mật và hiệu suất)</param>
        /// <returns>Password đã được hash</returns>
        public static string HashPassword(string password, int workFactor = 12)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password không được để trống", nameof(password));
            }

            return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
        }

        /// <summary>
        /// Verify password với password hash đã lưu
        /// </summary>
        /// <param name="password">Password plain text từ người dùng</param>
        /// <param name="hashedPassword">Password hash đã lưu trong database</param>
        /// <returns>True nếu password đúng, False nếu sai</returns>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                // Nếu có lỗi trong quá trình verify (ví dụ: hash không hợp lệ)
                return false;
            }
        }
    }
}

