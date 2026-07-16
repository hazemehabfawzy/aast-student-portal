using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.Entities;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize(Policy = "AdminOnly")]
public class CourseController : ControllerBase
{
    private readonly AppDbContext _context;

    public CourseController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCourses()
    {
        var courses = await _context.Courses
            .Where(c => c.IsActive)
            .Include(c => c.Department)
            .ToListAsync();
        return Ok(courses);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] Course model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate department exists
        var deptExists = await _context.Departments.AnyAsync(d => d.Id == model.DepartmentId);
        if (!deptExists)
        {
            return BadRequest(new { message = "Department does not exist." });
        }

        model.Id = Guid.NewGuid();
        model.IsActive = true;

        await _context.Courses.AddAsync(model);
        await _context.SaveChangesAsync();

        return Created($"/api/courses/{model.Id}", model);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCourse(Guid id, [FromBody] Course model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound(new { message = "Course not found." });
        }

        // Validate department exists
        var deptExists = await _context.Departments.AnyAsync(d => d.Id == model.DepartmentId);
        if (!deptExists)
        {
            return BadRequest(new { message = "Department does not exist." });
        }

        course.Code = model.Code;
        course.Name = model.Name;
        course.CreditHours = model.CreditHours;
        course.DepartmentId = model.DepartmentId;
        course.SemesterNumber = model.SemesterNumber;
        course.PrerequisiteCode = model.PrerequisiteCode;
        course.IsActive = model.IsActive;

        _context.Courses.Update(course);
        await _context.SaveChangesAsync();

        return Ok(course);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCourse(Guid id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound(new { message = "Course not found." });
        }

        // Reject if any active Section references this course
        var hasActiveSections = await _context.Sections
            .AnyAsync(s => s.CourseId == id && s.IsActive);
        
        if (hasActiveSections)
        {
            return BadRequest(new { message = "Cannot delete course because it has active sections referencing it." });
        }

        course.IsActive = false;
        _context.Courses.Update(course);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Course soft-deleted successfully." });
    }
}
