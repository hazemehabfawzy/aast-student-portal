using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortal.Api.Services;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "student")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("Name identifier claim is missing or invalid GUID.");
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var studentId = GetCurrentUserId();
        var profile = await _studentService.GetProfileAsync(studentId);
        if (profile == null)
        {
            return NotFound(new { message = "Student profile not found." });
        }
        return Ok(profile);
    }

    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule()
    {
        var studentId = GetCurrentUserId();
        var schedule = await _studentService.GetScheduleAsync(studentId);
        return Ok(schedule);
    }

    [HttpGet("grades")]
    public async Task<IActionResult> GetGrades()
    {
        var studentId = GetCurrentUserId();
        var grades = await _studentService.GetGradesAsync(studentId);
        return Ok(grades);
    }

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance()
    {
        var studentId = GetCurrentUserId();
        var records = await _studentService.GetAttendanceRecordsAsync(studentId);
        return Ok(records);
    }
}
