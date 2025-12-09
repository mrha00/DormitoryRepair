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
            // 📱 支持用户名或手机号登录
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username || u.PhoneNumber == dto.Username);
            
            if (user == null)
            {
                // 生产环境中不应暴露用户是否存在
                return Unauthorized(new { message = "用户名/手机号或密码错误" });
            }
            
            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            }
            catch (Exception ex)
            {
                // 记录日志但不暴露具体错误信息
                return Unauthorized(new { message = "用户名或密码错误" });
            }
            
            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "用户名或密码错误" });
            }

            // ✅ 查询用户的角色和权限
            var permissions = new List<string>();
            
            // 方法1：通过UserRoles表查询（用于有UserRoles关联的用户）
            var permissionsFromUserRoles = await (from ur in _context.UserRoles
                                    join rp in _context.RolePermissions on ur.RoleId equals rp.RoleId
                                    join p in _context.Permissions on rp.PermissionId equals p.Id
                                    where ur.UserId == user.Id
                                    select p.Name)
                                    .Distinct()
                                    .ToListAsync();
            permissions.AddRange(permissionsFromUserRoles);
            
            // 方法2：根据User.Role字段查询（用于直接设置角色的用户）
            if (user.Role == "Admin")
            {
                // 管理员拥有所有权限
                permissions = new List<string> 
                { 
                    "CreateOrder", 
                    "ViewOwnOrders", 
                    "ViewAllOrders", 
                    "ManageUsers", 
                    "AssignOrder",
                    "ProcessOrder",
                    "CompleteOrder"
                };
            }
            else if (user.Role == "Maintainer")
            {
                // 维修工可以处理和完成工单
                permissions = new List<string> 
                { 
                    "ViewOwnOrders", 
                    "ProcessOrder", 
                    "CompleteOrder" 
                };
            }
            else if (user.Role == "Student")
            {
                // 学生可以创建和查看自己的工单
                permissions = new List<string> 
                { 
                    "CreateOrder", 
                    "ViewOwnOrders" 
                };
            }

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