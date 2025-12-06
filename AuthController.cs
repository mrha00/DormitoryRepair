﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SmartDormitoryRepair.Api.Data;
using Microsoft.EntityFrameworkCore;
using SmartDormitoryRepair.Domain;

namespace SmartDormitoryRepair.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public AuthController(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "用户名或密码错误" });
            }

            // ✅ 查询用户的角色和权限
            var permissions = await (from ur in _context.UserRoles
                                     join rp in _context.RolePermissions on ur.RoleId equals rp.RoleId
                                     join p in _context.Permissions on rp.PermissionId equals p.Id
                                     where ur.UserId == user.Id
                                     select p.Name)
                                    .Distinct()
                                    .ToListAsync();

            var token = GenerateJwtToken(user.Username, user.Role);
            
            return Ok(new { 
                token, 
                user = new { user.Id, user.Username, user.Role },
                permissions  // ✅ 返回权限列表
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // 1. 检查用户名是否已存在
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            {
                return BadRequest(new { message = "用户名已存在" });
            }

            // 2. 创建新用户（默认角色为学生）
            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Student"
            };

            // 3. 保存到数据库
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "注册成功", userId = user.Id });
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

    // DTO类
    public class RegisterDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}