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
                // 如果数据库中已经有用户，则不添加测试用户
                if (context.Users.Any())
                {
                    return;
                }

                // 添加默认管理员用户
                context.Users.Add(
                    new User
                    {
                        Username = "admin",
                        PasswordHash = "$2a$11$KHLIPm3f2AipGAKax9Ym6Oh3x3A23A93WGNCDO/4riexaJWo6Z.xS",
                        Role = "Admin"
                    }
                );

                context.SaveChanges();
            }
        }
    }
}