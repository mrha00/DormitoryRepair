namespace SmartDormitoryRepair.Domain
{
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!; // CreateOrder, ViewOrders, ManageUsers
        public string Description { get; set; } = null!;
    }
}