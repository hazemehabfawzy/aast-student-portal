using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("public")]
    public IActionResult GetPublic()
    {
        return Ok(new { message = "This is a public endpoint. API is working!" });
    }

    [HttpGet("protected")]
    [Authorize]
    public IActionResult GetProtected()
    {
        var username = User.Identity?.Name ?? "Unknown";
        return Ok(new { message = $"Hello {username}, you are authenticated!" });
    }
}
