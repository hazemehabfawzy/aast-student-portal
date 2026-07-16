using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.Entities;
using StudentPortal.Api.Services.Implementations;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api/sections")]
public class SectionController : ControllerBase
{
    private readonly AppDbContext _context;

    public SectionController(AppDbContext context)
    {
        _context = context;
    }

    private string GetCurrentKeycloakId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
               ?? User.FindFirst("sub")?.Value 
               ?? throw new UnauthorizedAccessException("User identification claim is missing.");
    }

    [HttpGet("/api/instructor/sections")]
    [Authorize(Policy = "InstructorOnly")]
    public async Task<IActionResult> GetInstructorSections()
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == keycloakId);
            if (instructor == null)
            {
                return StatusCode(403, new { message = "Instructor record not found." });
            }

            var sections = await _context.Sections
                .Include(s => s.Course)
                .Include(s => s.Instructor)
                .Include(s => s.Semester)
                .Where(s => s.InstructorId == instructor.Id && s.IsActive)
                .Select(s => new
                {
                    s.Id,
                    CourseCode = s.Course!.Code,
                    CourseName = s.Course.Name,
                    s.ScheduleJson,
                    s.Capacity,
                    HasFaceAttendance = _context.Enrollments.Any(e => e.SectionId == s.Id && e.FaceAttendanceEnabled && !e.IsWithdrawn)
                })
                .ToListAsync();
            return Ok(sections);
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

    [HttpGet("available")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> GetAvailableSections()
    {
        var keycloakId = GetCurrentKeycloakId();
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.KeycloakId == keycloakId);
        if (student == null) return Unauthorized();

        var passedCourseCodes = await _context.Results
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Section)
                    .ThenInclude(s => s!.Course)
            .Where(r => r.Enrollment!.StudentId == student.Id
                     && r.Published
                     && r.LetterGrade != "F"
                     && r.LetterGrade != null)
            .Select(r => r.Enrollment!.Section!.Course!.Code)
            .Distinct()
            .ToListAsync();

        var passedSemesterNumbers = await _context.Results
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Section)
                    .ThenInclude(s => s!.Course)
            .Where(r => r.Enrollment!.StudentId == student.Id
                     && r.Published
                     && r.LetterGrade != "F"
                     && r.LetterGrade != null)
            .Select(r => r.Enrollment!.Section!.Course!.SemesterNumber)
            .ToListAsync();

        var currentSemesterNumber = passedSemesterNumbers.Count > 0
            ? Math.Min(passedSemesterNumbers.Max() + 1, 10)
            : 1;

        var regPeriod = await _context.RegistrationPeriods
            .Where(p => p.IsOpen)
            .FirstOrDefaultAsync();

        var sections = await _context.Sections
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .Include(s => s.Semester)
            .Include(s => s.Enrollments)
            .Where(s => s.IsActive)
            .Select(s => new
            {
                id = s.Id,
                s.ScheduleJson,
                s.Capacity,
                courseCode = s.Course!.Code,
                courseName = s.Course.Name,
                credits = s.Course.CreditHours,
                semester = s.Semester!.Name,
                semesterNumber = s.Course.SemesterNumber,
                prerequisiteCode = s.Course.PrerequisiteCode,
                instructorName = s.Instructor != null
                    ? s.Instructor.FullName : "TBA",
                enrolledCount = s.Enrollments.Count,
                alreadyEnrolled = s.Enrollments
                    .Any(e => e.StudentId == student.Id),
                enrollmentId = s.Enrollments
                    .Where(e => e.StudentId == student.Id)
                    .Select(e => (Guid?)e.Id)
                    .FirstOrDefault(),
            })
            .OrderBy(s => s.semesterNumber)
            .ToListAsync();

        var sectionsWithPrereq = sections.Select(s => new
        {
            s.id,
            s.ScheduleJson,
            s.Capacity,
            s.courseCode,
            s.courseName,
            s.credits,
            s.semester,
            s.semesterNumber,
            s.prerequisiteCode,
            s.instructorName,
            prerequisiteMet = PrerequisiteHelper.IsPrerequisiteMet(s.prerequisiteCode, passedCourseCodes),
            s.enrolledCount,
            s.alreadyEnrolled,
            s.enrollmentId,
        }).ToList();

        return Ok(new
        {
            sections = sectionsWithPrereq,
            passedCourses = passedCourseCodes,
            currentSemesterNumber,
            registrationOpen = regPeriod != null
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetSections([FromQuery] string? semesterId, [FromServices] IRegistrationService registrationService)
    {
        var keycloakId = GetCurrentKeycloakId();
        
        Guid resolvedSemesterId;
        if (!string.IsNullOrEmpty(semesterId) && Guid.TryParse(semesterId, out var sId))
        {
            resolvedSemesterId = sId;
        }
        else
        {
            var currentSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsCurrent);
            if (currentSemester == null)
            {
                return BadRequest(new { message = "No current semester found and invalid semesterId provided." });
            }
            resolvedSemesterId = currentSemester.Id;
        }

        if (User.IsInRole("student"))
        {
            try
            {
                var response = await registrationService.GetAvailableSectionsAsync(keycloakId, resolvedSemesterId);
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

        if (User.IsInRole("admin"))
        {
            var sections = await _context.Sections
                .Include(s => s.Course)
                .Include(s => s.Instructor)
                .Include(s => s.Semester)
                .ToListAsync();
            return Ok(sections);
        }
        else if (User.IsInRole("instructor"))
        {
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == keycloakId);
            if (instructor == null)
            {
                return StatusCode(403, new { message = "Instructor record not found." });
            }

            var sections = await _context.Sections
                .Include(s => s.Course)
                .Include(s => s.Instructor)
                .Include(s => s.Semester)
                .Where(s => s.InstructorId == instructor.Id)
                .ToListAsync();
            return Ok(sections);
        }

        return Forbid();
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateSection([FromBody] Section model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate InstructorId exists
        var instructorExists = await _context.Instructors.AnyAsync(i => i.Id == model.InstructorId);
        if (!instructorExists)
        {
            return BadRequest(new { message = "Instructor does not exist." });
        }

        // Validate CourseId exists
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == model.CourseId);
        if (!courseExists)
        {
            return BadRequest(new { message = "Course does not exist." });
        }

        // Validate SemesterId exists
        var semesterExists = await _context.Semesters.AnyAsync(s => s.Id == model.SemesterId);
        if (!semesterExists)
        {
            return BadRequest(new { message = "Semester does not exist." });
        }

        model.Id = Guid.NewGuid();
        model.IsActive = true;

        await _context.Sections.AddAsync(model);
        await _context.SaveChangesAsync();

        return Created($"/api/sections/{model.Id}", model);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateSection(Guid id, [FromBody] Section model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var section = await _context.Sections.FindAsync(id);
        if (section == null)
        {
            return NotFound(new { message = "Section not found." });
        }

        // Validate InstructorId exists
        var instructorExists = await _context.Instructors.AnyAsync(i => i.Id == model.InstructorId);
        if (!instructorExists)
        {
            return BadRequest(new { message = "Instructor does not exist." });
        }

        // Validate CourseId exists
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == model.CourseId);
        if (!courseExists)
        {
            return BadRequest(new { message = "Course does not exist." });
        }

        // Validate SemesterId exists
        var semesterExists = await _context.Semesters.AnyAsync(s => s.Id == model.SemesterId);
        if (!semesterExists)
        {
            return BadRequest(new { message = "Semester does not exist." });
        }

        section.CourseId = model.CourseId;
        section.InstructorId = model.InstructorId;
        section.SemesterId = model.SemesterId;
        section.ScheduleJson = model.ScheduleJson;
        section.Capacity = model.Capacity;
        section.IsActive = model.IsActive;

        _context.Sections.Update(section);
        await _context.SaveChangesAsync();

        return Ok(section);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteSection(Guid id)
    {
        var section = await _context.Sections.FindAsync(id);
        if (section == null)
        {
            return NotFound(new { message = "Section not found." });
        }

        section.IsActive = false;
        _context.Sections.Update(section);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Section soft-deleted successfully." });
    }
}
