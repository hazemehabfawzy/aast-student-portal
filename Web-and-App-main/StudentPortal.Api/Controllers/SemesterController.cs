using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/semesters")]
[Authorize]
public class SemesterController : ControllerBase
{
    private readonly AppDbContext _context;

    public SemesterController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns all semesters. Used by admin section management and student registration.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSemesters()
    {
        var semesters = await _context.Semesters
            .OrderByDescending(s => s.StartDate)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.StartDate,
                s.EndDate,
                s.IsCurrent
            })
            .ToListAsync();

        return Ok(semesters);
    }
}
