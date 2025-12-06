namespace SmartDormitoryRepair.Domain
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!; // Admin, Repairman, Student
        public string Description { get; set; } = null!;
    }
}