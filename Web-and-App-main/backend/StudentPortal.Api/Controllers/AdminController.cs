using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortal.Api.Models;
using StudentPortal.Api.Services;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _adminService.GetStatsAsync();
        return Ok(stats);
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportUsers([FromBody] IEnumerable<BulkImportItemDto> items)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var importedCount = await _adminService.BulkImportUsersAsync(items);
        return Ok(new { message = $"Import completed. Successfully imported {importedCount} users." });
    }

    // --- Course CRUD ---

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses()
    {
        var courses = await _adminService.GetCoursesAsync();
        return Ok(courses);
    }

    [HttpGet("courses/{id:int}")]
    public async Task<IActionResult> GetCourse(int id)
    {
        var course = await _adminService.GetCourseByIdAsync(id);
        if (course == null)
        {
            return NotFound(new { message = $"Course with ID {id} not found." });
        }
        return Ok(course);
    }

    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse([FromBody] CourseCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var course = await _adminService.CreateCourseAsync(dto);
        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
    }

    [HttpPut("courses/{id:int}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] CourseUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updatedCourse = await _adminService.UpdateCourseAsync(id, dto);
        if (updatedCourse == null)
        {
            return NotFound(new { message = $"Course with ID {id} not found." });
        }
        return Ok(updatedCourse);
    }

    [HttpDelete("courses/{id:int}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var success = await _adminService.DeleteCourseAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Course with ID {id} not found." });
        }
        return Ok(new { message = "Course deleted successfully." });
    }

    // --- Section CRUD ---

    [HttpGet("sections")]
    public async Task<IActionResult> GetSections()
    {
        var sections = await _adminService.GetSectionsAsync();
        return Ok(sections);
    }

    [HttpGet("sections/{id:int}")]
    public async Task<IActionResult> GetSection(int id)
    {
        var section = await _adminService.GetSectionByIdAsync(id);
        if (section == null)
        {
            return NotFound(new { message = $"Section with ID {id} not found." });
        }
        return Ok(section);
    }

    [HttpPost("sections")]
    public async Task<IActionResult> CreateSection([FromBody] SectionCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var section = await _adminService.CreateSectionAsync(dto);
        return CreatedAtAction(nameof(GetSection), new { id = section.Id }, section);
    }

    [HttpPut("sections/{id:int}")]
    public async Task<IActionResult> UpdateSection(int id, [FromBody] SectionUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updatedSection = await _adminService.UpdateSectionAsync(id, dto);
        if (updatedSection == null)
        {
            return NotFound(new { message = $"Section with ID {id} not found." });
        }
        return Ok(updatedSection);
    }

    [HttpDelete("sections/{id:int}")]
    public async Task<IActionResult> DeleteSection(int id)
    {
        var success = await _adminService.DeleteSectionAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Section with ID {id} not found." });
        }
        return Ok(new { message = "Section deleted successfully." });
    }
}
