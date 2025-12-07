using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SmartDormitoryRepair.Api.Data;
using SmartDormitoryRepair.Api.Hubs;
using SmartDormitoryRepair.Api.Services;
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
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly CacheService _cache;

        public OrdersController(
            AppDbContext context, 
            IHubContext<NotificationHub> hubContext,
            CacheService cache)
        {
            _context = context;
            _hubContext = hubContext;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult> GetOrders(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? status = null,
            [FromQuery] bool assignedToMe = false)
        {
            // 🚀 生成缓存键（根据查询参数）
            var currentUsername = User.Identity?.Name ?? "anonymous";
            var cacheKey = $"orders:{currentUsername}:page{page}:size{pageSize}:status{status}:assigned{assignedToMe}";
            
            // 🚀 尝试从缓存获取
            var cachedResult = _cache.Get<object>(cacheKey);
            if (cachedResult != null)
            {
                return Ok(cachedResult);
            }
            
            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }
            
            // 👥 如果请求只看分配给自己的工单
            if (assignedToMe)
            {
                if (!string.IsNullOrEmpty(currentUsername))
                {
                    var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
                    if (currentUser != null)
                    {
                        query = query.Where(o => o.AssignedTo == currentUser.Id || o.Creator == currentUsername);
                    }
                }
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new { items, total, page, pageSize };
            
            // 🚀 缓存10秒（热点数据）
            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(10));
            
            return Ok(result);
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
            
            // 🔍 查询维修工姓名
            string? assignedToName = null;
            if (order.AssignedTo.HasValue)
            {
                var maintainer = await _context.Users.FindAsync(order.AssignedTo.Value);
                assignedToName = maintainer?.Username;
            }
            
            // 📦 返回包含维修工姓名的数据
            var result = new
            {
                order.Id,
                order.Title,
                order.Description,
                order.Location,
                order.Creator,
                order.Status,
                order.ImageUrl,
                order.CreateTime,
                order.AssignedTo,
                AssignedToName = assignedToName // 维修工姓名
            };
            
            return Ok(result);
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
            
            // 🚀 清除缓存（简单实现，生产环境建议Redis）
            _cache.RemoveByPrefix("orders:");

            // 推送通知给管理员：有新工单提交
            var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();
            foreach (var admin in admins)
            {
                await _hubContext.Clients.Group($"user_{admin.Username}")
                    .SendAsync("ReceiveNotification", $"有新工单提交：{order.Title}", new { orderId = order.Id, title = order.Title });
            }

            return Ok(new { orderId = order.Id, message = "工单创建成功", imageUrl });
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("工单不存在");
            
            // 👑 管理员可以任意修改状态，普通用户需要遵循状态转换规则
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && !IsValidStatusTransition(order.Status, dto.Status))
                return BadRequest("非法的状态转换");
            
            order.Status = dto.Status;
            await _context.SaveChangesAsync();
            
            // 🚀 清除缓存
            _cache.RemoveByPrefix("orders:");
            
            // 推送通知给工单创建者：状态已更新
            await _hubContext.Clients.Group($"user_{order.Creator}")
                .SendAsync("ReceiveNotification", $"您的工单《{order.Title}》状态已更新为：{GetStatusText(dto.Status)}", new { orderId = order.Id, title = order.Title });
            
            return Ok(new { message = "状态更新成功" });
        }

        [HttpGet("maintainers")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult> GetMaintainers()
        {
            // 🚀 维修工列表缓存30分钟（人员变动不频繁）
            const string cacheKey = "maintainers:list";
            
            var maintainers = await _cache.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    return await _context.Users
                        .Where(u => u.Role == "Maintainer")
                        .Select(u => new { u.Id, u.Username })
                        .ToListAsync();
                },
                TimeSpan.FromMinutes(30)
            );
            
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
            
            // 💾 保存消息到数据库
            var notification = new Notification
            {
                ReceiverUsername = maintainer.Username,
                Title = "📢 新工单通知",
                Message = $"您有新的工单待处理：{order.Title}",
                Type = "OrderAssigned",
                RelatedOrderId = order.Id,
                IsRead = false,
                CreateTime = DateTime.Now
            };
            _context.Notifications.Add(notification);
            
            await _context.SaveChangesAsync();
            
            // 🚀 清除缓存
            _cache.RemoveByPrefix("orders:");
            
            // ✅ 推送实时通知给维修工
            var message = $"您有新的工单待处理：{order.Title}";
            await _hubContext.Clients.Group($"user_{maintainer.Username}")
                .SendAsync("ReceiveNotification", message, new { orderId = order.Id, title = order.Title });
            
            Console.WriteLine($"Notification sent to {maintainer.Username} for order {order.Id}");
            
            return Ok(new { message = "指派成功并已通知维修工" });
        }

        // 🗑️ 删除工单（仅管理员）
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("工单不存在");
            
            // 删除相关通知
            var relatedNotifications = await _context.Notifications
                .Where(n => n.RelatedOrderId == id)
                .ToListAsync();
            _context.Notifications.RemoveRange(relatedNotifications);
            
            // 删除工单
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "工单已删除" });
        }

        // 🔄 更换维修工（仅管理员）
        [HttpPut("{id}/reassign")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ReassignOrder(int id, [FromBody] AssignOrderDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("工单不存在");
            
            var newMaintainer = await _context.Users.FindAsync(dto.MaintainerId);
            if (newMaintainer == null) return NotFound("维修人员不存在");
            
            var oldMaintainerId = order.AssignedTo;
            order.AssignedTo = dto.MaintainerId;
            
            // 💾 保存消息到数据库
            var notification = new Notification
            {
                ReceiverUsername = newMaintainer.Username,
                Title = "🔄 工单重新指派",
                Message = $"工单《{order.Title}》已被重新指派给您，请及时处理",
                Type = "OrderReassigned",
                RelatedOrderId = order.Id,
                IsRead = false,
                CreateTime = DateTime.Now
            };
            _context.Notifications.Add(notification);
            
            await _context.SaveChangesAsync();
            
            // ✅ 推送实时通知给新维修工
            var message = $"工单《{order.Title}》已被重新指派给您";
            await _hubContext.Clients.Group($"user_{newMaintainer.Username}")
                .SendAsync("ReceiveNotification", message, new { orderId = order.Id, title = order.Title });
            
            return Ok(new { message = "已重新指派给新的维修工" });
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

        private string GetStatusText(string status)
        {
            return status switch
            {
                "Pending" => "待处理",
                "Processing" => "处理中",
                "Completed" => "已完成",
                _ => status
            };
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