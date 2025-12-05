namespace SmartDormitoryRepair.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 暂时只保留必要的服务
            builder.Services.AddControllers(); // 恢复这个，因为你要用控制器

            var app = builder.Build();

            app.MapControllers(); // 映射控制器路由
            app.Run("http://0.0.0.0:5000"); // 指定HTTP端口

            app.Run();
        }
    }
}
