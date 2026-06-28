using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    private string GetCurrentKeycloakId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
               ?? User.FindFirst("sub")?.Value 
               ?? throw new UnauthorizedAccessException("User identification claim is missing.");
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            string role = "user";
            string name = User.FindFirst("name")?.Value ?? User.FindFirst("preferred_username")?.Value ?? "User";
            Guid? linkedId = null;

            if (User.IsInRole("admin"))
            {
                role = "admin";
            }
            else if (User.IsInRole("instructor"))
            {
                role = "instructor";
                var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == keycloakId);
                if (instructor != null)
                {
                    linkedId = instructor.Id;
                    name = instructor.FullName;
                }
            }
            else if (User.IsInRole("student"))
            {
                role = "student";
                var student = await _context.Students.FirstOrDefaultAsync(s => s.KeycloakId == keycloakId);
                if (student != null)
                {
                    linkedId = student.Id;
                    name = student.FullName;
                }
            }

            return Ok(new
            {
                role,
                name,
                linkedId
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
