using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.Entities;
using StudentPortal.Api.Services.Implementations;

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

    [HttpPost("create")]
    public async Task<IActionResult> CreateInstructorAccount(
        [FromBody] CreateInstructorAccountDto dto,
        [FromServices] KeycloakAdminService keycloak)
    {
        string keycloakId;
        try
        {
            keycloakId = await keycloak.CreateUser(
                dto.Username, dto.Password,
                dto.FirstName, dto.LastName,
                dto.Email, "instructor");
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }

        var defaultDept = (await _context.Departments.Select(d => d.Id).FirstOrDefaultAsync());

        var instructor = new Instructor
        {
            Id           = Guid.NewGuid(),
            FullName     = $"{dto.FirstName} {dto.LastName}",
            KeycloakId   = keycloakId,
            DepartmentId = dto.DepartmentId != Guid.Empty ? dto.DepartmentId : defaultDept,
        };
        _context.Instructors.Add(instructor);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            instructor.Id,
            instructor.FullName,
            instructor.KeycloakId,
            message = $"Login: {dto.Username} / {dto.Password}"
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateInstructor(Guid id, [FromBody] UpdateInstructorDto dto)
    {
        var instructor = await _context.Instructors.FindAsync(id);
        if (instructor == null) return NotFound();
        if (dto.FullName != null) instructor.FullName = dto.FullName;
        if (dto.DepartmentId.HasValue && dto.DepartmentId.Value != Guid.Empty)
            instructor.DepartmentId = dto.DepartmentId.Value;
        await _context.SaveChangesAsync();
        return Ok(instructor);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteInstructor(Guid id)
    {
        var instructor = await _context.Instructors.FindAsync(id);
        if (instructor == null) return NotFound();
        _context.Instructors.Remove(instructor);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Instructor deleted" });
    }
}

public class UpdateInstructorDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public Guid? DepartmentId { get; set; }
}

public class CreateInstructorAccountDto
{
    public string Username     { get; set; } = "";
    public string Password     { get; set; } = "";
    public string FirstName    { get; set; } = "";
    public string LastName     { get; set; } = "";
    public string Email        { get; set; } = "";
    public Guid   DepartmentId { get; set; }
}
