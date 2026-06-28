using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly AppDbContext _db;
    public DoctorsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetDoctors() =>
        Ok(await _db.Instructors
            .Select(i => new { i.Id, i.FullName, i.KeycloakId, i.DepartmentId })
            .ToListAsync());
}
