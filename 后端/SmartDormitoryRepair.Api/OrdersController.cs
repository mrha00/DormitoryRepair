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

            order.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "状态更新成功" });
        }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = null!;
    }
}