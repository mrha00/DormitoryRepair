using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDormitoryRepair.Api.Data;
using SmartDormitoryRepair.Domain;
using System.Security.Claims;

namespace SmartDormitoryRepair.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Where(u => u.Username == username)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Role,
                    u.PhoneNumber,
                    u.IsActive
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            return Ok(new { data = user });
        }

        /// <summary>
        /// 修改当前用户密码
        /// </summary>
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            // 验证旧密码
            if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "原密码错误" });
            }

            // 更新密码
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "密码修改成功" });
        }

        /// <summary>
        /// 更新个人资料（手机号）
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            // 更新手机号
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                user.PhoneNumber = dto.PhoneNumber;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "个人资料更新成功" });
        }

        // ========== 以下是管理员功能 ==========

        /// <summary>
        /// 创建新用户（仅管理员）
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            // 验证用户名是否已存在
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            {
                return BadRequest(new { message = "用户名已存在" });
            }

            // 验证角色有效性
            var validRoles = new[] { "Admin", "Maintainer", "Student" };
            if (!validRoles.Contains(dto.Role))
            {
                return BadRequest(new { message = "无效的角色" });
            }

            // 创建新用户
            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                PhoneNumber = dto.PhoneNumber,
                IsActive = true,
                CreateTime = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "用户创建成功", userId = user.Id });
        }

        /// <summary>
        /// 获取所有用户列表（仅管理员）
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? role = null,
            [FromQuery] string? keyword = null,
            [FromQuery] bool? isActive = null)
        {
            var query = _context.Users.AsQueryable();

            // 筛选角色
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }

            // 筛选状态
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            // 关键词搜索（用户名或手机号）
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(u => u.Username.Contains(keyword) || 
                                        (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword)));
            }

            var total = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Role,
                    u.PhoneNumber,
                    u.IsActive,
                    u.CreateTime
                })
                .ToListAsync();

            return Ok(new { data = new { items = users, total } });
        }

        /// <summary>
        /// 重置用户密码（仅管理员）
        /// </summary>
        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto? dto = null)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            // 防止重置管理员密码（除非是自己）
            var currentUsername = User.Identity?.Name;
            if (user.Role == "Admin" && user.Username != currentUsername)
            {
                return BadRequest(new { message = "不能重置其他管理员的密码" });
            }

            // 使用传入的密码，如果没有传入则使用默认密码 a123456
            var newPassword = dto?.Password ?? "a123456";
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"密码已重置为：{newPassword}" });
        }

        /// <summary>
        /// 修改用户角色（仅管理员）
        /// </summary>
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            // 防止修改管理员角色（除非是自己）
            var currentUsername = User.Identity?.Name;
            if (user.Role == "Admin" && user.Username != currentUsername)
            {
                return BadRequest(new { message = "不能修改其他管理员的角色" });
            }

            // 验证角色有效性
            var validRoles = new[] { "Admin", "Maintainer", "Student" };
            if (!validRoles.Contains(dto.Role))
            {
                return BadRequest(new { message = "无效的角色" });
            }

            user.Role = dto.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = "角色修改成功" });
        }

        /// <summary>
        /// 启用/禁用用户（仅管理员）
        /// </summary>
        [HttpPut("{id}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            // 防止禁用管理员账号（除非是自己）
            var currentUsername = User.Identity?.Name;
            if (user.Role == "Admin" && user.Username != currentUsername)
            {
                return BadRequest(new { message = "不能禁用其他管理员账号" });
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            var status = user.IsActive ? "启用" : "禁用";
            return Ok(new { message = $"账号已{status}" });
        }

        /// <summary>
        /// 删除用户（仅管理员）
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            // 防止删除管理员账号
            if (user.Role == "Admin")
            {
                return BadRequest(new { message = "不能删除管理员账号" });
            }

            // 检查是否有关联工单
            var hasOrders = await _context.Orders.AnyAsync(o => o.Creator == user.Username);
            if (hasOrders)
            {
                return BadRequest(new { message = "该用户有关联工单，无法删除" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "用户已删除" });
        }
    }

    // DTO类
    public class CreateUserDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? PhoneNumber { get; set; }
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class UpdateProfileDto
    {
        public string? PhoneNumber { get; set; }
    }

    public class UpdateRoleDto
    {
        public string Role { get; set; } = null!;
    }

    public class ResetPasswordDto
    {
        public string? Password { get; set; }
    }
}
