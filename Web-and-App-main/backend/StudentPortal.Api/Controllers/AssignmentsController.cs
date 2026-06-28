using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/assignments")]
public class AssignmentsController : ControllerBase
{
    private readonly StudentPortalDbContext _context;

    public AssignmentsController(StudentPortalDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAssignments([FromQuery] int? sectionId)
    {
        var q = _context.Assignments
            .Include(a => a.Instructor)
                .ThenInclude(i => i.User)
            .Include(a => a.Attachments)
            .AsQueryable();

        if (sectionId.HasValue) q = q.Where(a => a.SectionId == sectionId.Value);

        var list = await q.OrderByDescending(a => a.DueDate).Select(a => new
        {
            a.Id,
            a.Title,
            a.Body,
            a.DueDate,
            a.SectionId,
            Instructor = a.Instructor != null ? new { a.Instructor.Id, FullName = a.Instructor.User != null ? a.Instructor.User.FullName : string.Empty } : null,
            Attachments = a.Attachments.Select(att => new { att.Id, att.FileName, att.ContentType })
        }).ToListAsync();

        return Ok(list);
    }

    public class CreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public DateTime? DueDate { get; set; }
        public int? SectionId { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "instructor")]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateDto dto)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var instructorId)) return Forbid();

        var assignment = new Assignment
        {
            Title = dto.Title,
            Body = dto.Body,
            DueDate = dto.DueDate,
            SectionId = dto.SectionId,
            InstructorId = instructorId
        };

        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();

        return Ok(new { assignment.Id });
    }

    [HttpPost("{assignmentId}/attachments")]
    [Authorize(Roles = "instructor")]
    public async Task<IActionResult> UploadAttachment(int assignmentId, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }

        var assignment = await _context.Assignments.FindAsync(assignmentId);
        if (assignment == null)
        {
            return NotFound();
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Assignments");
        Directory.CreateDirectory(uploadsFolder);

        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, storedFileName);

        await using (var fileStream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(fileStream);
        }

        var attachment = new AssignmentAttachment
        {
            AssignmentId = assignmentId,
            FileName = file.FileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            FilePath = filePath
        };

        _context.AssignmentAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        return Ok(new { attachment.Id });
    }

    [HttpGet("{assignmentId}/attachments/{attachmentId}")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadAttachment(int assignmentId, int attachmentId)
    {
        var attachment = await _context.AssignmentAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.AssignmentId == assignmentId);

        if (attachment == null)
        {
            return NotFound();
        }

        if (!System.IO.File.Exists(attachment.FilePath))
        {
            return NotFound();
        }

        return PhysicalFile(attachment.FilePath, attachment.ContentType, attachment.FileName);
    }
}
