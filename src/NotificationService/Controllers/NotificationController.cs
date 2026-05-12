using Microsoft.AspNetCore.Mvc;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "NotificationService is running" });
}
