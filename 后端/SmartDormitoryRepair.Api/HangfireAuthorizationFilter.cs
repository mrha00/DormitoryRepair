using Hangfire.Dashboard;

namespace SmartDormitoryRepair.Api
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            
            // ✅ 开发环境下允许直接访问（无需认证）
            var env = httpContext.RequestServices.GetService<IWebHostEnvironment>();
            if (env?.IsDevelopment() == true)
            {
                return true; // 开发环境允许所有人访问
            }
            
            // 生产环境下只允许管理员访问
            return httpContext.User.IsInRole("Admin");
        }
    }
}
