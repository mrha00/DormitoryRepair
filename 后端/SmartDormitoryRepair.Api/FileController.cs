using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartDormitoryRepair.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 需要登录才能上传
public class FileController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileController> _logger;

    public FileController(IWebHostEnvironment env, ILogger<FileController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<ActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "文件不能为空" });

        // 限制文件大小：5MB
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "文件大小不能超过5MB" });

        // 验证文件类型（仅允许图片）
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = "仅支持图片格式（jpg、png、gif、webp）" });

        try
        {
            // 生成唯一文件名
            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads");

            // 创建上传目录
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, fileName);

            // 保存文件
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 返回相对路径
            var url = $"/uploads/{fileName}";
            _logger.LogInformation("文件上传成功: {FileName}, 大小: {FileSize}KB", fileName, file.Length / 1024);

            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败");
            return StatusCode(500, new { message = "文件上传失败：" + ex.Message });
        }
    }
}
