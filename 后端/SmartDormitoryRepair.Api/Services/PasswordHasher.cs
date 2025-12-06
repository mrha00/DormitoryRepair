using BCrypt.Net;

namespace SmartDormitoryRepair.Api.Services
{
    public static class PasswordHasher
    {
        // 生成密码哈希
        public static string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // 验证密码
        public static bool Verify(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
