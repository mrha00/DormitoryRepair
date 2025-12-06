namespace SmartDormitoryRepair.Domain.DTOs
{
    public class CreateOrderDto
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Location { get; set; } = null!;  // 宿舍位置，如"3号楼301"
    }
}
