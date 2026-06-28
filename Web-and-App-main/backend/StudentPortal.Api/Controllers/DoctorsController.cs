using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly StudentPortalDbContext _context;

    public DoctorsController(StudentPortalDbContext context)
    {
        _context = context;
    }

    // GET: /api/doctors
    // Returns a simple list of instructors for authenticated users (students/instructors)
    [HttpGet]
    public async Task<IActionResult> GetDoctors()
    {
        var doctors = await _context.Instructors
            .Include(i => i.User)
            .Select(i => new
            {
                Id = i.Id,
                Title = i.Title,
                FullName = i.User != null ? i.User.FullName : null,
                Email = i.User != null ? i.User.Email : null,
                Phone = i.User != null ? i.User.Phone : null
            })
            .ToListAsync();

        return Ok(doctors);
    }
}
