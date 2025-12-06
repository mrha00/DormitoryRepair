using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDormitoryRepair.Api.Data;
using SmartDormitoryRepair.Domain;

namespace SmartDormitoryRepair.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取当前用户的消息列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetNotifications(
            [FromQuery] bool? isRead = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "未登录" });
            }

            var query = _context.Notifications
                .Where(n => n.ReceiverUsername == username);

            // 筛选已读/未读
            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            var total = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                items = notifications,
                total,
                page,
                pageSize
            });
        }

        /// <summary>
        /// 获取未读消息数量
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult> GetUnreadCount()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "未登录" });
            }

            var count = await _context.Notifications
                .Where(n => n.ReceiverUsername == username && !n.IsRead)
                .CountAsync();

            return Ok(new { count });
        }

        /// <summary>
        /// 标记消息为已读
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "未登录" });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.ReceiverUsername == username);

            if (notification == null)
            {
                return NotFound(new { message = "消息不存在" });
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadTime = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "已标记为已读" });
        }

        /// <summary>
        /// 标记所有消息为已读
        /// </summary>
        [HttpPut("read-all")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "未登录" });
            }

            var unreadNotifications = await _context.Notifications
                .Where(n => n.ReceiverUsername == username && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadTime = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"已标记 {unreadNotifications.Count} 条消息为已读" });
        }

        /// <summary>
        /// 删除消息
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(int id)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "未登录" });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.ReceiverUsername == username);

            if (notification == null)
            {
                return NotFound(new { message = "消息不存在" });
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "消息已删除" });
        }
    }
}
