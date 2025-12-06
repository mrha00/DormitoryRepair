using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDormitoryRepair.Api.Data;
using SmartDormitoryRepair.Domain;
using SmartDormitoryRepair.Domain.DTOs;

namespace SmartDormitoryRepair.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 需要认证
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null)
        {
            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { items, total, page, pageSize });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetOrderById(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("工单不存在");
            
            // 权限检查：学生只能看自己的工单
            if (User.IsInRole("Student") && order.Creator != User.Identity?.Name)
            {
                return Forbid("无权查看他人工单");
            }
            
            return Ok(order);
        }

        [HttpPost]
        public async Task<ActionResult> CreateOrder([FromForm] CreateOrderDto dto, IFormFile? image)
        {
            var username = User.Identity?.Name ?? "Anonymous";
            
            // 保存图片(如果上传了)
            string? imageUrl = null;
            if (image != null)
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                
                imageUrl = $"/uploads/{fileName}";
            }

            var order = new Order
            {
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                Creator = username,
                Status = "Pending",
                ImageUrl = imageUrl
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { orderId = order.Id, message = "工单创建成功", imageUrl });
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("工单不存在");
            
            // 状态合法性检查
            if (!IsValidStatusTransition(order.Status, dto.Status))
                return BadRequest("非法的状态转换");
            
            order.Status = dto.Status;
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "状态更新成功" });
        }

        [HttpGet("maintainers")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult> GetMaintainers()
        {
            var maintainers = await _context.Users
                .Where(u => u.Role == "Maintainer")
                .Select(u => new { u.Id, u.Username })
                .ToListAsync();
            
            return Ok(maintainers);
        }

        [HttpPost("{id}/assign")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult> AssignOrder(int id, [FromBody] AssignOrderDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("工单不存在");
            
            var maintainer = await _context.Users.FindAsync(dto.MaintainerId);
            if (maintainer == null) return NotFound("维修人员不存在");
            
            order.AssignedTo = dto.MaintainerId;
            order.Status = "Processing";
            
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "指派成功" });
        }

        private bool IsValidStatusTransition(string current, string next)
        {
            // 规则：Pending → Processing → Completed
            var allowed = new Dictionary<string, string[]>
            {
                { "Pending", new[] { "Processing" } },
                { "Processing", new[] { "Completed" } },
                { "Completed", Array.Empty<string>() }
            };
            return allowed.ContainsKey(current) && allowed[current].Contains(next);
        }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = null!;
    }

    public class AssignOrderDto
    {
        public int MaintainerId { get; set; }
    }
}