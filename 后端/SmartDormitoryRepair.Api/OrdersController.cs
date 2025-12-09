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
                return StatusCode(403, new { message = "无权查看他人工单" });
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
        public async Task<ActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var username = User.Identity?.Name ?? "Anonymous";

            var order = new Order
            {
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                Creator = username,
                Status = "Pending",
                ImageUrl = dto.ImageUrl  // 使用前端上传后返回的URL
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

            return Ok(new { orderId = order.Id, message = "工单创建成功", imageUrl = order.ImageUrl });
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("工单不存在");
            
            var currentUsername = User.Identity?.Name ?? "";
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
            
            var isAdmin = User.IsInRole("Admin");
            
            // ⚠️ 业务规则（对所有人有效，包括管理员）：
            // 如果要改为"处理中"或"已完成"，必须先分配维修工
            if ((dto.Status == "Processing" || dto.Status == "Completed") && !order.AssignedTo.HasValue)
            {
                // 特殊情况：维修工点击"开始处理"时，自动分配给自己
                if (User.IsInRole("Maintainer") && dto.Status == "Processing")
                {
                    if (currentUser != null)
                    {
                        order.AssignedTo = currentUser.Id;
                        Console.WriteLine($"✅ 工单 #{order.Id} 自动分配给维修工: {currentUser.Username}");
                    }
                    else
                    {
                        return BadRequest(new { message = "系统错误：无法获取当前用户信息" });
                    }
                }
                // 👑 管理员可以直接分配维修工并修改状态
                else if (isAdmin && dto.Status == "Processing" && dto.AssignTo.HasValue)
                {
                    // 检查指定的维修工是否存在
                    var maintainer = await _context.Users.FindAsync(dto.AssignTo.Value);
                    if (maintainer == null || maintainer.Role != "Maintainer")
                    {
                        return BadRequest(new { message = "指定的用户不是有效的维修工" });
                    }
                    
                    order.AssignedTo = dto.AssignTo.Value;
                    Console.WriteLine($"✅ 工单 #{order.Id} 由管理员分配给维修工: {maintainer.Username}");
                }
                else
                {
                    // 🚫 普通用户不能直接修改为"处理中"或"已完成"，必须先分配维修工
                    return BadRequest(new { message = "请先分配维修工再修改状态！" });
                }
            }
            
            // 👑 管理员可以任意修改状态（但已经通过上面的检查），普通用户需要遵循状态转换规则
            if (!isAdmin && !IsValidStatusTransition(order.Status, dto.Status))
                return BadRequest("非法的状态转换");
            
            // 🚫 维修工只能标记完成自己负责的工单
            if (User.IsInRole("Maintainer") && dto.Status == "Completed")
            {
                if (order.AssignedTo != currentUser?.Id)
                {
                    return StatusCode(403, new { message = "您只能标记完成自己负责的工单" });
                }
            }
            
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
            // ✅ 修复：指派时不修改状态，由维修工手动点击“开始处理”
            // order.Status = "Processing";  // 删除此行
            
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
        public int? AssignTo { get; set; }  // 管理员在修改状态时可以同时指定维修工
    }

    public class AssignOrderDto
    {
        public int MaintainerId { get; set; }
    }
}