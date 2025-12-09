using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartDormitoryRepair.Api.Data;
using SmartDormitoryRepair.Api;
using SmartDormitoryRepair.Api.Hubs;
using SmartDormitoryRepair.Api.Services;
using SmartDormitoryRepair.Api.Filters; // 🔥 AOP操作日志
using System.Text;
using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("JWT Key未配置");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new Exception("JWT Issuer未配置");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new Exception("JWT Audience未配置");

// 添加CORS服务
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("Default"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("Default"))
    ));

// 🚀 添加内存缓存服务
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CacheService>();

// 🔥 注册AOP日志过滤器
builder.Services.AddScoped<OperationLogFilter>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        
        // SignalR 需要的 JWT 配置
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR(); // 添加 SignalR 服务

// 注册定时任务服务
builder.Services.AddScoped<TimeoutOrderChecker>();

// 添加 Hangfire 服务
try
{
    builder.Services.AddHangfire(config =>
        config.UseStorage(new MySqlStorage(
            builder.Configuration.GetConnectionString("Default"),
            new MySqlStorageOptions
            {
                QueuePollInterval = TimeSpan.FromSeconds(15),
                JobExpirationCheckInterval = TimeSpan.FromHours(1),
                CountersAggregateInterval = TimeSpan.FromMinutes(5),
                PrepareSchemaIfNecessary = true,
                DashboardJobListLimit = 50000,
                TransactionTimeout = TimeSpan.FromMinutes(1),
                TablesPrefix = "Hangfire"
            }
        )));
    builder.Services.AddHangfireServer();
}
catch (Exception ex)
{
    Console.WriteLine("Hangfire setup failed: " + ex.Message);
}

builder.Services.AddControllers(options =>
{
    // 🔥 全局添加AOP日志过滤器
    options.Filters.Add<OperationLogFilter>();
});

var app = builder.Build();

// 使用CORS中间件
app.UseCors("AllowFrontend");

// 配置静态文件服务
app.UseDefaultFiles(); // 使 / 自动返回 wwwroot 下的 index.html
app.UseStaticFiles(); // 允许访问 wwwroot 下的静态文件

// 初始化种子数据（在所有环境中都运行）
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    SeedData.Initialize(services);
}

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard （需要 Admin 角色）
try
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}
catch (Exception ex)
{
    Console.WriteLine("Hangfire dashboard failed: " + ex.Message);
}

app.MapHub<NotificationHub>("/notificationHub"); // 映射 SignalR Hub
app.MapControllers();

// 前端 SPA 路由回退到 index.html
app.MapFallbackToFile("/index.html");

// 创建定时任务：每天检查超时48小时未处理的工单
try
{
    RecurringJob.AddOrUpdate<TimeoutOrderChecker>(
        "CheckTimeoutOrders",
        checker => checker.CheckTimeoutOrders(),
        Cron.Daily()
    );
}
catch (Exception ex)
{
    Console.WriteLine("Hangfire recurring job failed: " + ex.Message);
}

// 兼容 IIS 与自托管：若环境已提供端口，则使用默认 app.Run()
var providedUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
var providedPort = Environment.GetEnvironmentVariable("ASPNETCORE_PORT");
if (!string.IsNullOrEmpty(providedUrls) || !string.IsNullOrEmpty(providedPort))
{
    app.Run();
}
else
{
    app.Run("http://0.0.0.0:8080");
}
