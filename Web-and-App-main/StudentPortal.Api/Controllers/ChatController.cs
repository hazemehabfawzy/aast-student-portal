using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.DTOs.Requests;
using StudentPortal.Api.Models.Entities;

using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public ChatController(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    [HttpGet("sections/{sectionId}")]
    public async Task<IActionResult> GetMessages(
        string sectionId,
        [FromQuery] int limit = 50)
    {
        if (!Guid.TryParse(sectionId, out var parsedSectionId))
        {
            return BadRequest(new { message = "Invalid section id." });
        }

        var messages = await _db.ChatMessages
            .Where(m => m.SectionId == parsedSectionId)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .OrderBy(m => m.SentAt)
            .Select(m => new
            {
                m.Id,
                m.SenderName,
                m.SenderRole,
                m.Message,
                m.SentAt,
                m.IsRead
            })
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPost("sections/{sectionId}")]
    public async Task<IActionResult> SendMessage(
        string sectionId,
        [FromBody] SendMessageDto dto)
    {
        if (!Guid.TryParse(sectionId, out var parsedSectionId))
        {
            return BadRequest(new { message = "Invalid section id." });
        }

        if (string.IsNullOrWhiteSpace(dto.Message))
        {
            return BadRequest(new { message = "Message cannot be empty." });
        }

        var keycloakId = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(keycloakId))
        {
            return Unauthorized();
        }

        var student = await _db.Students
            .FirstOrDefaultAsync(s => s.KeycloakId == keycloakId);
        var instructor = await _db.Instructors
            .FirstOrDefaultAsync(i => i.KeycloakId == keycloakId);

        var senderName = student?.FullName
            ?? instructor?.FullName ?? "Unknown";
        var senderRole = instructor != null ? "instructor" : "student";

        var msg = new ChatMessage
        {
            SectionId = parsedSectionId,
            SenderKeycloakId = keycloakId,
            SenderName = senderName,
            SenderRole = senderRole,
            Message = dto.Message.Trim(),
        };

        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();

        // Send notifications to enrolled students in this section
        try
        {
            var section = await _db.Sections
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.Id == parsedSectionId);
            
            var courseCode = section?.Course?.Code ?? "Course";

            var enrollments = await _db.Enrollments
                .Where(e => e.SectionId == parsedSectionId && !e.IsWithdrawn)
                .ToListAsync();

            foreach (var enrollment in enrollments)
            {
                // Skip the student who sent the message
                if (student != null && enrollment.StudentId == student.Id)
                    continue;

                await _notificationService.SendPushAsync(
                    enrollment.StudentId,
                    "chat",
                    "New Chat Message",
                    $"{senderName} in {courseCode}: {dto.Message.Trim()}"
                );
            }
        }
        catch
        {
            // Do not fail the send message request if notification dispatch fails
        }

        return Ok(new
        {
            msg.Id,
            msg.SenderName,
            msg.SenderRole,
            msg.Message,
            msg.SentAt,
            msg.IsRead
        });
    }
}
