using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.Entities;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db) => _db = db;

    // ─── Sections ────────────────────────────────────────────────────────────

    [HttpGet("sections")]
    public async Task<IActionResult> GetSections() =>
        Ok(await _db.Sections
            .Where(s => s.IsActive)
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .Include(s => s.Semester)
            .Select(s => new
            {
                s.Id,
                s.CourseId,
                CourseCode    = s.Course!.Code,
                CourseName    = s.Course.Name,
                s.InstructorId,
                InstructorName = s.Instructor != null ? s.Instructor.FullName : null,
                s.SemesterId,
                SemesterName  = s.Semester != null ? s.Semester.Name : null,
                s.ScheduleJson,
                s.Capacity,
                s.IsActive,
                StudentCount  = _db.Enrollments.Count(e => e.SectionId == s.Id && !e.IsWithdrawn)
            })
            .ToListAsync());

    [HttpPost("sections")]
    public async Task<IActionResult> CreateSection([FromBody] CreateSectionRequest dto)
    {
        var semesterId = dto.SemesterId != Guid.Empty
            ? dto.SemesterId
            : (await _db.Semesters.FirstOrDefaultAsync(s => s.IsCurrent))?.Id
              ?? (await _db.Semesters.FirstOrDefaultAsync())?.Id
              ?? Guid.NewGuid();

        var section = new Section
        {
            Id           = Guid.NewGuid(),
            CourseId     = dto.CourseId,
            InstructorId = dto.InstructorId != Guid.Empty ? dto.InstructorId
                           : (await _db.Instructors.Select(i => i.Id).FirstOrDefaultAsync()),
            SemesterId   = semesterId,
            ScheduleJson = dto.ScheduleJson ?? "[]",
            Capacity     = dto.Capacity > 0 ? dto.Capacity : 30,
            IsActive     = true,
        };

        _db.Sections.Add(section);
        await _db.SaveChangesAsync();
        return Ok(new { section.Id, section.CourseId, section.InstructorId, section.Capacity });
    }

    // ─── Enrollment ──────────────────────────────────────────────────────────

    [HttpPost("sections/{sectionId:guid}/enroll/{studentId:guid}")]
    public async Task<IActionResult> EnrollStudent(Guid sectionId, Guid studentId)
    {
        if (!await _db.Sections.AnyAsync(s => s.Id == sectionId))
            return NotFound(new { message = "Section not found." });

        if (!await _db.Students.AnyAsync(s => s.Id == studentId))
            return NotFound(new { message = "Student not found." });

        if (await _db.Enrollments.AnyAsync(e => e.SectionId == sectionId && e.StudentId == studentId && !e.IsWithdrawn))
            return BadRequest(new { message = "Student is already enrolled in this section." });

        var enrollment = new Enrollment
        {
            Id        = Guid.NewGuid(),
            SectionId = sectionId,
            StudentId = studentId,
        };
        _db.Enrollments.Add(enrollment);

        var result = new Result { Id = Guid.NewGuid(), EnrollmentId = enrollment.Id };
        _db.Results.Add(result);

        await _db.SaveChangesAsync();
        return Ok(new { enrollment.Id, enrollment.SectionId, enrollment.StudentId });
    }

    [HttpDelete("sections/{sectionId:guid}/enroll/{studentId:guid}")]
    public async Task<IActionResult> UnenrollStudent(Guid sectionId, Guid studentId)
    {
        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.SectionId == sectionId && e.StudentId == studentId && !e.IsWithdrawn);

        if (enrollment == null)
            return NotFound(new { message = "Enrollment not found." });

        enrollment.IsWithdrawn  = true;
        enrollment.WithdrawnAt  = DateTime.UtcNow;
        _db.Enrollments.Update(enrollment);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Student unenrolled successfully." });
    }

    [HttpGet("sections/{sectionId:guid}/students")]
    public async Task<IActionResult> GetSectionStudents(Guid sectionId)
    {
        var enrolled = await _db.Enrollments
            .Include(e => e.Student)
            .Where(e => e.SectionId == sectionId && !e.IsWithdrawn)
            .Select(e => new { e.Id, e.StudentId, e.Student!.FullName, e.Student.StudentNumber, e.Student.Email })
            .ToListAsync();
        return Ok(enrolled);
    }

    // ─── Assign Instructor ───────────────────────────────────────────────────

    [HttpPut("sections/{sectionId:guid}/instructor/{instructorId:guid}")]
    public async Task<IActionResult> AssignInstructor(Guid sectionId, Guid instructorId)
    {
        var section = await _db.Sections.FindAsync(sectionId);
        if (section == null) return NotFound(new { message = "Section not found." });

        if (!await _db.Instructors.AnyAsync(i => i.Id == instructorId))
            return NotFound(new { message = "Instructor not found." });

        section.InstructorId = instructorId;
        _db.Sections.Update(section);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Instructor assigned successfully.", sectionId, instructorId });
    }

    // ─── Instructors ─────────────────────────────────────────────────────────

    [HttpGet("instructors")]
    public async Task<IActionResult> GetInstructors() =>
        Ok(await _db.Instructors
            .Select(i => new { i.Id, i.FullName, i.DepartmentId })
            .ToListAsync());

    // ─── Students (for enrollment panel) ─────────────────────────────────────

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents() =>
        Ok(await _db.Students
            .Select(s => new { s.Id, s.FullName, s.StudentNumber, s.Email })
            .ToListAsync());
}

public class CreateSectionRequest
{
    public Guid CourseId     { get; set; }
    public Guid InstructorId { get; set; }
    public Guid SemesterId   { get; set; }
    public string? Name      { get; set; }
    public string? Room      { get; set; }
    public int Capacity      { get; set; }
    public string? ScheduleJson { get; set; }
}
