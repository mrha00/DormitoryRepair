using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartDormitoryRepair.Api.Data;
using SmartDormitoryRepair.Api.Hubs;
using SmartDormitoryRepair.Domain; // ✅ 添加引用

namespace SmartDormitoryRepair.Api.Services
{
    public class TimeoutOrderChecker
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public TimeoutOrderChecker(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CheckTimeoutOrders()
        {
            Console.WriteLine($"🔍 开始检查超时工单... 时间: {DateTime.Now}");
            
            // 检查超过48小时未处理的工单
            var timeoutTime = DateTime.Now.AddHours(-48);
            var timeoutOrders = await _context.Orders
                .Where(o => o.Status == "Pending" && o.CreateTime < timeoutTime)
                .ToListAsync();

            Console.WriteLine($"✅ 找到 {timeoutOrders.Count} 个超时工单");

            foreach (var order in timeoutOrders)
            {
                // 计算超时天数
                var hoursElapsed = (DateTime.Now - order.CreateTime).TotalHours;
                var daysElapsed = Math.Floor(hoursElapsed / 24);
                
                // 推送通知给所有管理员
                var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();
                Console.WriteLine($"📢 找到 {admins.Count} 个管理员");
                
                // 根据超时时间显示不同的消息
                string notificationMessage;
                if (daysElapsed >= 2)
                {
                    notificationMessage = $"⚠️ 工单{order.Id}《{order.Title}》已超时{daysElapsed}天未处理！";
                }
                else
                {
                    notificationMessage = $"⚠️ 工单{order.Id}《{order.Title}》已超时{hoursElapsed:F1}小时未处理！";
                }
                
                foreach (var admin in admins)
                {
                    // ✅ 保存通知到数据库（消息中心）
                    var notification = new Notification
                    {
                        ReceiverUsername = admin.Username,
                        Title = "⚠️ 工单超时提醒",
                        Message = notificationMessage,
                        Type = "OrderTimeout",
                        RelatedOrderId = order.Id,
                        IsRead = false,
                        CreateTime = DateTime.Now
                    };
                    _context.Notifications.Add(notification);
                    
                    // 推送实时通知到用户组
                    await _hubContext.Clients.Group($"user_{admin.Username}")
                        .SendAsync("ReceiveNotification", 
                            notificationMessage, 
                            new { orderId = order.Id, title = order.Title });
                    
                    Console.WriteLine($"✅ 已推送通知给管理员: {admin.Username}");
                }
                
                // 保存所有通知到数据库
                await _context.SaveChangesAsync();

                Console.WriteLine($"⚠️ 超时工单: ID={order.Id}, 创建时间={order.CreateTime}, 超时={hoursElapsed:F1}小时");
            }

            Console.WriteLine($"✅ 检查完成，总计找到 {timeoutOrders.Count} 个超时工单\n");
        }
    }
}