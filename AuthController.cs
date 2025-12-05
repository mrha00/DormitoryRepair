using Microsoft.AspNetCore.Mvc;

namespace SmartDormitoryRepair.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase

    {
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "API运行正常", time = DateTime.Now });
        }

    }

}
