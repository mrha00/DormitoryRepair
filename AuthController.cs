using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartDormitoryRepair.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            var adminHash = "$2a$11$KHLIPm3f2AipGAKax9Ym6Oh3x3A23A93WGNCDO/4riexaJWo6Z.xS";

            if (dto.Username == "admin" && BCrypt.Net.BCrypt.Verify(dto.Password, adminHash))
            {
                var token = GenerateJwtToken(dto.Username, "Admin");
                return Ok(new { token });
            }
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        private string GenerateJwtToken(string username, string role)
        {
            var jwtKey = _config["Jwt:Key"] ?? "";
            var jwtIssuer = _config["Jwt:Issuer"] ?? "";
            var jwtAudience = _config["Jwt:Audience"] ?? "";
            var expireMinutes = double.Parse(_config["Jwt:ExpireMinutes"] ?? "60");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(expireMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
