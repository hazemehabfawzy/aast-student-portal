using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortal.Api.Models.DTOs.Requests;
using StudentPortal.Api.Models.Entities;
using StudentPortal.Api.Services.Implementations;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly AppDbContext _context;

    public AttendanceController(IAttendanceService attendanceService, AppDbContext context)
    {
        _attendanceService = attendanceService;
        _context = context;
    }

    private string GetCurrentKeycloakId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
               ?? User.FindFirst("sub")?.Value 
               ?? throw new UnauthorizedAccessException("User identification claim is missing.");
    }

    [HttpPost("attendance/sessions")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var response = await _attendanceService.CreateSessionAsync(keycloakId, request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("attendance/sessions/{id:guid}/code")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetSessionCode(Guid id)
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var newCode = await _attendanceService.RotateSessionCodeAsync(keycloakId, id);
            return Ok(new { currentCode = newCode, expiresAt = DateTime.UtcNow.AddSeconds(30) });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("attendance/check-in")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // FIRST: Check for X-Client-Platform header
        if (!Request.Headers.TryGetValue("X-Client-Platform", out var platformHeader) || 
            platformHeader.ToString() != "mobile")
        {
            return StatusCode(403, new { message = "Attendance check-in is only available from the mobile app." });
        }

        try
        {
            var keycloakId = GetCurrentKeycloakId();
            await _attendanceService.CheckInAsync(keycloakId, request);
            return Ok(new { message = "Successfully checked in" });
        }
        catch (RateLimitException ex)
        {
            return StatusCode(429, new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("attendance/sessions/{id:guid}/close")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> CloseSession(Guid id)
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            await _attendanceService.CloseSessionAsync(keycloakId, id);
            return Ok(new { message = "Session closed successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("sections/{sectionId:guid}/attendance")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetSectionAttendance(Guid sectionId)
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var response = await _attendanceService.GetSectionAttendanceAsync(keycloakId, sectionId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("students/me/attendance")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> GetMyAttendance()
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var response = await _attendanceService.GetStudentMeAttendanceAsync(keycloakId);
            return Ok(response);
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

    [HttpGet("attendance/active-sessions")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> GetActiveSessions()
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var student = await _context.Students.FirstOrDefaultAsync(s => s.KeycloakId == keycloakId);
            if (student == null)
            {
                return Unauthorized(new { message = "Student record not found." });
            }

            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == student.Id)
                .Select(e => e.SectionId)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var activeSessions = await _context.AttendanceSessions
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Course)
                .Where(s => enrollments.Contains(s.SectionId) && now >= s.StartTime && now <= s.EndTime)
                .Select(s => new {
                    sessionId  = s.Id,
                    courseCode = s.Section!.Course!.Code,
                    courseName = s.Section.Course.Name,
                    method     = s.Method
                })
                .ToListAsync();

            return Ok(activeSessions);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("attendance/check-in/face")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> FaceStudentCheckIn([FromBody] FaceCheckInDto dto)
    {
        if (!Request.Headers.TryGetValue("X-Client-Platform", out var platform) || platform.ToString() != "mobile")
            return StatusCode(403, new { message = "Face check-in is only available from the mobile app." });

        var keycloakId = GetCurrentKeycloakId();
        var student = await _context.Students.FirstOrDefaultAsync(s => s.KeycloakId == keycloakId);
        if (student == null) return Unauthorized(new { message = "Student record not found." });

        try
        {
            var result = await _attendanceService.FaceStudentCheckInAsync(student.Id, dto.SessionId, dto.Image);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)    { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)               { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("attendance/sessions/{sessionId:guid}/face-checkin")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> InstructorFaceCheckIn(Guid sessionId, [FromBody] FaceCheckInDto dto)
    {
        try
        {
            var result = await _attendanceService.FaceCheckInAsync(sessionId, dto.Image);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)    { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)               { return BadRequest(new { message = ex.Message }); }
    }
}
