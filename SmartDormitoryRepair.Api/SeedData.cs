using Microsoft.EntityFrameworkCore;
using SmartDormitoryRepair.Api.Data;
using SmartDormitoryRepair.Domain;

namespace SmartDormitoryRepair.Api
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new AppDbContext(
                serviceProvider.GetRequiredService<
                    DbContextOptions<AppDbContext>>()))
            {
                try
                {
                    // 添加角色（如果不存在）
                    if (!context.Roles.Any())
                    {
                        var adminRole = new Role { Name = "Admin", Description = "管理员，拥有所有权限" };
                        var repairmanRole = new Role { Name = "Repairman", Description = "维修工，处理工单" };
                        var studentRole = new Role { Name = "Student", Description = "学生，提交报修" };
                        
                        context.Roles.Add(adminRole);
                        context.Roles.Add(repairmanRole);
                        context.Roles.Add(studentRole);
                        context.SaveChanges();
                        Console.WriteLine("Roles added successfully.");
                    }

                    // 添加权限（如果不存在）
                    if (!context.Permissions.Any())
                    {
                        var permissions = new Permission[]
                        {
                            new Permission { Name = "CreateOrder", Description = "创建报修工单" },
                            new Permission { Name = "ViewOwnOrders", Description = "查看自己的工单" },
                            new Permission { Name = "ViewAllOrders", Description = "查看所有工单" },
                            new Permission { Name = "ManageUsers", Description = "管理用户" },
                            new Permission { Name = "AssignOrder", Description = "指派工单" }
                        };
                        context.Permissions.AddRange(permissions);
                        context.SaveChanges();
                        Console.WriteLine("Permissions added successfully.");
                    }

                    // 添加角色权限关联（如果不存在）
                    if (!context.RolePermissions.Any())
                    {
                        // 先检查角色和权限是否存在
                        var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
                        if (adminRole != null)
                        {
                            var permissions = context.Permissions.ToList();
                            if (permissions.Count >= 5)
                            {
                                var rolePermissions = new RolePermission[]
                                {
                                    new RolePermission { RoleId = adminRole.Id, PermissionId = permissions[0].Id },
                                    new RolePermission { RoleId = adminRole.Id, PermissionId = permissions[1].Id },
                                    new RolePermission { RoleId = adminRole.Id, PermissionId = permissions[2].Id },
                                    new RolePermission { RoleId = adminRole.Id, PermissionId = permissions[3].Id },
                                    new RolePermission { RoleId = adminRole.Id, PermissionId = permissions[4].Id }
                                };
                                context.RolePermissions.AddRange(rolePermissions);
                                context.SaveChanges();
                                Console.WriteLine("RolePermissions added successfully.");
                            }
                        }
                    }

                    // 确保至少有一个管理员用户
                    if (!context.Users.Any())
                    {
                        context.Users.Add(
                            new User
                            {
                                Username = "admin",
                                PasswordHash = "$2a$11$KHLIPm3f2AipGAKax9Ym6Oh3x3A23A93WGNCDO/4riexaJWo6Z.xS",
                                Role = "Admin"
                            }
                        );
                        context.SaveChanges();
                        Console.WriteLine("Admin user added successfully.");
                    }

                    // 给admin用户分配Admin角色（如果不存在关联）
                    var adminUser = context.Users.FirstOrDefault(u => u.Username == "admin");
                    var adminRoleObj = context.Roles.FirstOrDefault(r => r.Name == "Admin");
                    if (adminUser != null && adminRoleObj != null && 
                        !context.UserRoles.Any(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRoleObj.Id))
                    {
                        var userRole = new UserRole { UserId = adminUser.Id, RoleId = adminRoleObj.Id };
                        context.UserRoles.Add(userRole);
                        context.SaveChanges();
                        Console.WriteLine("UserRole association added successfully.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing seed data: {ex.Message}");
                    throw;
                }
            }
        }
    }
}