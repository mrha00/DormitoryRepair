using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using System.Text.Json;

namespace SmartDormitoryRepair.Api.Filters;

/// <summary>
/// AOP操作日志过滤器
/// 自动记录Controller方法调用、参数、返回值、执行时间等信息
/// </summary>
public class OperationLogFilter : IActionFilter, IAsyncActionFilter
{
    private readonly ILogger<OperationLogFilter> _logger;
    private const string StopwatchKey = "ActionStopwatch";

    public OperationLogFilter(ILogger<OperationLogFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // 方法执行前：记录开始时间
        var stopwatch = Stopwatch.StartNew();
        context.HttpContext.Items[StopwatchKey] = stopwatch;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // 方法执行后：记录日志
        if (context.HttpContext.Items[StopwatchKey] is Stopwatch stopwatch)
        {
            stopwatch.Stop();
            LogOperation(context, stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext executingContext, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // 执行实际Action
            var executedContext = await next();
            stopwatch.Stop();

            // 记录日志
            LogOperation(executedContext, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogError(executingContext, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    private void LogOperation(ActionExecutedContext context, long elapsedMilliseconds)
    {
        var request = context.HttpContext.Request;
        var user = context.HttpContext.User;
        var username = user?.Identity?.Name ?? "Anonymous";
        var ipAddress = context.HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
        var action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
        var method = $"{controller}Controller.{action}";

        // 记录请求参数
        var parameterInfo = context.ActionDescriptor.Parameters.Count > 0
            ? JsonSerializer.Serialize(context.ActionDescriptor.Parameters.Select(p => new { ParamName = p.Name, ParamType = p.ParameterType.Name }))
            : "No Parameters";

        // 构建日志对象
        var logEntry = new
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            User = username,
            IP = ipAddress,
            Method = method,
            HttpMethod = request.Method,
            Path = request.Path.Value,
            QueryString = request.QueryString.Value,
            Parameters = parameterInfo,
            StatusCode = context.HttpContext.Response.StatusCode,
            ExecutionTime = $"{elapsedMilliseconds}ms",
            Status = context.Exception == null ? "Success" : "Failed"
        };

        var logMessage = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });
        
        if (context.Exception == null)
        {
            _logger.LogInformation("📝 操作日志:\n{LogMessage}", logMessage);
        }
        else
        {
            _logger.LogError("❌ 操作失败:\n{LogMessage}", logMessage);
        }

        // 🚀 写入本地日志文件（生产环境使用MongoDB）
        WriteToFile(logMessage);
    }

    private void LogError(ActionExecutingContext context, long elapsedMilliseconds, Exception ex)
    {
        var request = context.HttpContext.Request;
        var user = context.HttpContext.User;
        var username = user?.Identity?.Name ?? "Anonymous";
        var ipAddress = context.HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
        var action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
        var method = $"{controller}Controller.{action}";

        var errorLogEntry = new
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            User = username,
            IP = ipAddress,
            Method = method,
            HttpMethod = request.Method,
            Path = request.Path.Value,
            ExecutionTime = $"{elapsedMilliseconds}ms",
            Status = "Failed",
            ErrorMessage = ex.Message,
            ErrorType = ex.GetType().Name,
            StackTrace = ex.StackTrace
        };

        var errorLogMessage = JsonSerializer.Serialize(errorLogEntry, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogError(ex, "❌ 操作异常日志:\n{ErrorLogMessage}", errorLogMessage);

        WriteToFile(errorLogMessage);
    }

    /// <summary>
    /// 写入本地日志文件
    /// 生产环境建议使用MongoDB、Elasticsearch等专业日志存储
    /// </summary>
    private void WriteToFile(string logMessage)
    {
        try
        {
            var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            if (!Directory.Exists(logsDir))
                Directory.CreateDirectory(logsDir);

            var fileName = $"operation_log_{DateTime.Now:yyyyMMdd}.txt";
            var filePath = Path.Combine(logsDir, fileName);

            // 异步写入文件，避免阻塞主线程
            Task.Run(() =>
            {
                try
                {
                    File.AppendAllText(filePath, logMessage + "\n" + new string('-', 100) + "\n");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "日志文件写入失败");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "日志文件创建失败");
        }
    }
}
