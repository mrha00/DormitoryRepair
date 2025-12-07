﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR 需要
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

builder.Services.AddControllers(options =>
{
    // 🔥 全局添加AOP日志过滤器
    options.Filters.Add<OperationLogFilter>();
});

var app = builder.Build();

// 使用CORS中间件
app.UseCors("AllowFrontend");

// 配置静态文件服务
app.UseStaticFiles(); // 允许访问wwwroot下的文件

// 初始化种子数据
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    SeedData.Initialize(services);
}

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard （需要 Admin 角色）
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapHub<NotificationHub>("/notificationHub"); // 映射 SignalR Hub
app.MapControllers();

// 创建定时任务：每小时检查超时24小时未处理的工单
RecurringJob.AddOrUpdate<TimeoutOrderChecker>(
    "CheckTimeoutOrders",
    checker => checker.CheckTimeoutOrders(),
    Cron.Hourly()
);

app.Run("http://0.0.0.0:5002");