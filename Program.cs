var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthorization();

// 直接写登录接口，bypass Controller
app.MapPost("/api/auth/login", (LoginDto dto) =>
{
    if (dto.Username == "admin" && dto.Password == "123456")
    {
        return Results.Ok(new { token = "fake-jwt-token" });
    }
    return Results.Unauthorized();
});

// 测试接口
app.MapGet("/api/auth/test", () => Results.Ok(new { message = "API运行正常", time = DateTime.Now }));

app.Run("http://0.0.0.0:5000");

public class LoginDto
{
    public string Username { get; set; }
    public string Password { get; set; }
}
