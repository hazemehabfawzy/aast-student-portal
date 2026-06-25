using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.DTOs.Requests;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api")]
public class NotificationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;

    public NotificationController(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    private string GetCurrentKeycloakId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
               ?? User.FindFirst("sub")?.Value 
               ?? throw new UnauthorizedAccessException("User identification claim is missing.");
    }

    private async Task<Guid> GetCurrentStudentIdAsync(string keycloakId)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.KeycloakId == keycloakId);
        if (student == null)
        {
            throw new UnauthorizedAccessException("Student record not found.");
        }
        return student.Id;
    }

    [HttpGet("students/me/notifications")]
    [HttpGet("notifications")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> GetMyNotifications()
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var studentId = await GetCurrentStudentIdAsync(keycloakId);

            var list = await _context.Notifications
                .Where(n => n.StudentId == studentId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(list);
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

    [HttpPut("notifications/{id:guid}/read")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var studentId = await GetCurrentStudentIdAsync(keycloakId);

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound(new { message = "Notification not found." });
            }

            if (notification.StudentId != studentId)
            {
                return StatusCode(403, new { message = "You do not own this notification." });
            }

            notification.IsRead = true;
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification marked as read." });
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

    [HttpPost("students/me/fcm-token")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var studentId = await GetCurrentStudentIdAsync(keycloakId);

            await _notificationService.RegisterDeviceTokenAsync(studentId, request.Token, request.Platform);
            return Ok(new { message = "FCM token registered successfully." });
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
}
