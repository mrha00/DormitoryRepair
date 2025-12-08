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
                            new Permission { Name = "AssignOrder", Description = "指派工单" },
                            new Permission { Name = "ProcessOrder", Description = "处理工单（开始维修）" },
                            new Permission { Name = "CompleteOrder", Description = "完成工单（标记完成）" }
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
                        // admin 用户
                        context.Users.Add(
                            new User
                            {
                                Username = "admin",
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                                Role = "Admin"
                            }
                        );
                        
                        // 添加维修工测试账号
                        context.Users.AddRange(
                            new User
                            {
                                Username = "张师傅",
                                PasswordHash = "$2a$11$KHLIPm3f2AipGAKax9Ym6Oh3x3A23A93WGNCDO/4riexaJWo6Z.xS", // admin123
                                Role = "Maintainer"
                            },
                            new User
                            {
                                Username = "李师傅",
                                PasswordHash = "$2a$11$KHLIPm3f2AipGAKax9Ym6Oh3x3A23A93WGNCDO/4riexaJWo6Z.xS", // admin123
                                Role = "Maintainer"
                            },
                            new User
                            {
                                Username = "王师傅",
                                PasswordHash = "$2a$11$KHLIPm3f2AipGAKax9Ym6Oh3x3A23A93WGNCDO/4riexaJWo6Z.xS", // admin123
                                Role = "Maintainer"
                            },
                            new User
                            {
                                Username = "刘师傅",
                                PasswordHash = "$2a$11$KHLIPm3f2AipGAKax9Ym6Oh3x3A23A93WGNCDO/4riexaJWo6Z.xS", // admin123
                                Role = "Maintainer"
                            }
                        );
                        
                        context.SaveChanges();
                        Console.WriteLine("Admin user and maintainers added successfully.");
                    }

                    // 给admin用户分配Admin角色（如果不存在关联）
                    var adminUserForRole = context.Users.FirstOrDefault(u => u.Username == "admin");
                    var adminRoleObj = context.Roles.FirstOrDefault(r => r.Name == "Admin");
                    if (adminUserForRole != null && adminRoleObj != null && 
                        !context.UserRoles.Any(ur => ur.UserId == adminUserForRole.Id && ur.RoleId == adminRoleObj.Id))
                    {
                        var userRole = new UserRole { UserId = adminUserForRole.Id, RoleId = adminRoleObj.Id };
                        context.UserRoles.Add(userRole);
                        context.SaveChanges();
                        Console.WriteLine("UserRole association added successfully.");
                    }

                    // 🔥 强制重置维修工账号（删除旧的，创建新的）
                    var maintainerNames = new[] { "张师傅", "李师傅", "王师傅", "刘师傅" };
                    
                    // 先删除所有现有的维修工账号
                    var existingMaintainers = context.Users.Where(u => maintainerNames.Contains(u.Username)).ToList();
                    if (existingMaintainers.Any())
                    {
                        context.Users.RemoveRange(existingMaintainers);
                        context.SaveChanges();
                        Console.WriteLine($"🗑️ 已删除 {existingMaintainers.Count} 个旧的维修工账号");
                    }
                    
                    // 🔑 强制重置admin用户密码（解决密码哈希过期问题）
                    var existingAdmin = context.Users.FirstOrDefault(u => u.Username == "admin");
                    if (existingAdmin != null)
                    {
                        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");  // 🔑 改为 admin123（8位）
                        existingAdmin.PasswordHash = newPasswordHash;
                        context.SaveChanges();
                        Console.WriteLine($"✅ 已重置admin密码：admin123");
                        Console.WriteLine($"🔑 新密码哈希: {newPasswordHash}");
                    }
                    
                    // 重新创建维修工账号（使用新生成的密码哈希）
                    var newPassword = BCrypt.Net.BCrypt.HashPassword("admin123");
                    Console.WriteLine($"🔑 新密码哈希: {newPassword}");
                    
                    foreach (var name in maintainerNames)
                    {
                        context.Users.Add(new User
                        {
                            Username = name,
                            PasswordHash = newPassword,
                            Role = "Maintainer"
                        });
                    }
                    context.SaveChanges();
                    Console.WriteLine($"✅ 已重新创建 {maintainerNames.Length} 个维修工账号");

                    // 🎓 新增学生测试账号（张三、李四、王五）
                    var studentNames = new[] { "张三", "李四", "王五" };
                    foreach (var name in studentNames)
                    {
                        if (!context.Users.Any(u => u.Username == name))
                        {
                            var studentPassword = BCrypt.Net.BCrypt.HashPassword("password123");
                            context.Users.Add(new User
                            {
                                Username = name,
                                PasswordHash = studentPassword,
                                Role = "Student"
                            });
                        }
                    }
                    context.SaveChanges();
                    Console.WriteLine($"✅ 学生账号检查完成（张三、李四、王五）");

                    // 🎓 添加学生测试工单（如果不存在）
                    var studentOrders = context.Orders.Where(o => 
                        o.Creator == "张三" || o.Creator == "李四" || o.Creator == "王五"
                    ).ToList();
                    
                    if (studentOrders.Count == 0)
                    {
                        var orders = new Order[]
                        {
                            // 🎓 张三的工单（2个）
                            new Order 
                            { 
                                Title = "宿舍空调不制冷", 
                                Description = "303宿舍空调只吹热风，不制冷，天气太热了", 
                                Location = "3号楼303室",
                                Creator = "张三", 
                                Status = "Pending",
                                CreateTime = DateTime.Now.AddHours(-2)
                            },
                            new Order 
                            { 
                                Title = "床铺板床板松动", 
                                Description = "上铺的板床有几块板子松了，晚上睡觉同吠响，影响休息", 
                                Location = "3号楼303室",
                                Creator = "张三", 
                                Status = "Processing",
                                CreateTime = DateTime.Now.AddDays(-1)
                            },
                            
                            // 🎓 李四的工单（2个）
                            new Order 
                            { 
                                Title = "窗户玻璃破损", 
                                Description = "阳台窗户玻璃出现裂纹，担心安全问题，请尽快维修", 
                                Location = "5号楼512室",
                                Creator = "李四", 
                                Status = "Pending",
                                CreateTime = DateTime.Now.AddHours(-5)
                            },
                            new Order 
                            { 
                                Title = "马桶不上水", 
                                Description = "卫生间马桶水箱不上水，无法正常使用，需要维修", 
                                Location = "5号楼512室",
                                Creator = "李四", 
                                Status = "Completed",
                                CreateTime = DateTime.Now.AddDays(-3)
                            },
                            
                            // 🎓 王五的工单（2个）
                            new Order 
                            { 
                                Title = "电风扇不转了", 
                                Description = "宿舍吊扇无法启动，按开关没有反应，可能是电机坏了", 
                                Location = "7号楼701室",
                                Creator = "王五", 
                                Status = "Processing",
                                CreateTime = DateTime.Now.AddHours(-8)
                            },
                            new Order 
                            { 
                                Title = "书桌抽屉门板掉落", 
                                Description = "书桌第二个抽屉的门板掉了，影响使用，希望维修或更换", 
                                Location = "7号楼701室",
                                Creator = "王五", 
                                Status = "Pending",
                                CreateTime = DateTime.Now.AddDays(-2)
                            }
                        };
                        context.Orders.AddRange(orders);
                        context.SaveChanges();
                        Console.WriteLine("✅ 测试工单添加成功（张三×2、李四×2、王五×2）");
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