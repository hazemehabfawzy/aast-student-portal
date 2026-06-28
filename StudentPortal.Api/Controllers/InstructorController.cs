using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/instructors")]
[Authorize(Policy = "AdminOnly")]
public class InstructorController : ControllerBase
{
    private readonly AppDbContext _context;

    public InstructorController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns all instructors. Used by admin panel to populate section form dropdowns.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllInstructors()
    {
        var instructors = await _context.Instructors
            .Include(i => i.Department)
            .Select(i => new
            {
                i.Id,
                i.FullName,
                i.KeycloakId,
                i.DepartmentId,
                DepartmentName = i.Department != null ? i.Department.Name : null
            })
            .ToListAsync();

        return Ok(instructors);
    }
}
