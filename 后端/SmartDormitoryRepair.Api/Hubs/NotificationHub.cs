using Microsoft.AspNetCore.SignalR;

namespace SmartDormitoryRepair.Api.Hubs
{
    public class NotificationHub : Hub
    {
        // 连接时自动加入用户组
        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{username}");
                // 生产环境中应避免记录敏感信息如连接ID
            }
            await base.OnConnectedAsync();
        }

        // 断开连接时自动移除用户组
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                // 生产环境中应避免记录敏感信息
            }
            await base.OnDisconnectedAsync(exception);
        }

        // 发送通知给特定用户
        public async Task SendToUser(string username, string message, object? data = null)
        {
            await Clients.Group($"user_{username}").SendAsync("ReceiveNotification", message, data);
        }

        // 心跳检测
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", DateTime.Now);
        }
    }
}