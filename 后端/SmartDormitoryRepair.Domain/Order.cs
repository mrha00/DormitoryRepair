namespace SmartDormitoryRepair.Domain
{
    public class Order
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;           // 报修标题
        public string Description { get; set; } = null!;    // 详细描述
        public string Location { get; set; } = null!;       // 宿舍位置
        public string Creator { get; set; } = null!;        // 报修人（用户名）
        public DateTime CreateTime { get; set; } = DateTime.Now; // 报修时间
        public string Status { get; set; } = "Pending";     // Pending/Processing/Completed
        public string? ImageUrl { get; set; }                // 图片路径
        public int? AssignedTo { get; set; }                // 指派给（UserId）
    }
}