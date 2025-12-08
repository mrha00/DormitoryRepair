namespace SmartDormitoryRepair.Domain
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? PhoneNumber { get; set; }  // 📱 手机号
        public bool IsActive { get; set; } = true;  // ✅ 账号状态（启用/禁用）
        public DateTime CreateTime { get; set; } = DateTime.Now;  // 📅 创建时间
    }
}