using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.Entities;

using System.Security.Claims;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/students")]
public class StudentController : ControllerBase
{
    private readonly AppDbContext _context;

    public StudentController(AppDbContext context)
    {
        _context = context;
    }

    private string GetCurrentKeycloakId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
               ?? User.FindFirst("sub")?.Value 
               ?? throw new UnauthorizedAccessException("User identification claim is missing.");
    }

    [HttpGet("me/profile")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> GetMyProfile()
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var student = await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.KeycloakId == keycloakId);

            if (student == null)
            {
                return StatusCode(403, new { message = "Student record not found." });
            }

            return Ok(student);
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

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetStudents(
        [FromQuery] string? name = null,
        [FromQuery] string? studentNumber = null,
        [FromQuery] string? department = null)
    {
        var query = _context.Students
            .Include(s => s.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(s => s.FullName.ToLower().Contains(name.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(studentNumber))
        {
            query = query.Where(s => s.StudentNumber.ToLower().Contains(studentNumber.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            // Match department ID (Guid) or department name (contains)
            if (Guid.TryParse(department, out var deptId))
            {
                query = query.Where(s => s.DepartmentId == deptId);
            }
            else
            {
                query = query.Where(s => s.Department != null && s.Department.Name.ToLower().Contains(department.ToLower()));
            }
        }

        var students = await query.ToListAsync();
        return Ok(students);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetStudentById(Guid id)
    {
        var student = await _context.Students
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
        {
            return NotFound(new { message = "Student not found." });
        }

        return Ok(student);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] Student model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var student = await _context.Students.FindAsync(id);
        if (student == null)
        {
            return NotFound(new { message = "Student not found." });
        }

        // Validate department exists
        var deptExists = await _context.Departments.AnyAsync(d => d.Id == model.DepartmentId);
        if (!deptExists)
        {
            return BadRequest(new { message = "Department does not exist." });
        }

        student.FullName = model.FullName;
        student.DateOfBirth = model.DateOfBirth;
        student.Phone = model.Phone;
        student.Address = model.Address;
        student.DepartmentId = model.DepartmentId;
        student.YearLevel = model.YearLevel;
        
        // Keep StudentNumber and KeycloakId as is (read-only/identity fields unless explicitly changed, but let's allow updating them if model provides non-empty ones, though normally identity is preserved. To be safe, let's keep them read-only since standard identity is preserved, or if user passes them, we don't modify unless desired. The requirement says: "admin can edit any field including DepartmentId and YearLevel.")
        if (!string.IsNullOrEmpty(model.StudentNumber) && model.StudentNumber != student.StudentNumber)
        {
            // Validate unique student number
            var exists = await _context.Students.AnyAsync(s => s.StudentNumber == model.StudentNumber && s.Id != id);
            if (exists)
            {
                return BadRequest(new { message = "Student number is already in use." });
            }
            student.StudentNumber = model.StudentNumber;
        }

        _context.Students.Update(student);
        await _context.SaveChangesAsync();

        return Ok(student);
    }
}
