using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.DTOs.Requests;
using StudentPortal.Api.Models.Entities;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Services.Implementations;

public class ResultService : IResultService
{
    private readonly AppDbContext _context;
    private readonly IGradingService _gradingService;
    private readonly INotificationService _notificationService;

    public ResultService(AppDbContext context, IGradingService gradingService, INotificationService notificationService)
    {
        _context = context;
        _gradingService = gradingService;
        _notificationService = notificationService;
    }

    public async Task<object> GetMyResultsAsync(string studentKeycloakId, Guid? semesterId = null)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.KeycloakId == studentKeycloakId);
        if (student == null)
        {
            throw new UnauthorizedAccessException("Student record not found.");
        }

        var query = _context.Enrollments
            .Include(e => e.Section)
                .ThenInclude(s => s!.Semester)
            .Include(e => e.Section)
                .ThenInclude(s => s!.Course)
            .Include(e => e.Result)
            .Where(e => e.StudentId == student.Id);

        if (semesterId.HasValue)
        {
            query = query.Where(e => e.Section!.SemesterId == semesterId.Value);
        }

        var enrollments = await query.ToListAsync();
        var scales = await _context.GradeScales.ToListAsync();

        var resultList = enrollments.Select(e =>
        {
            var r = e.Result;
            var scale = r != null ? scales.FirstOrDefault(gs => gs.Letter == r.LetterGrade) : null;
            return new
            {
                ResultId = r?.Id,
                CourseCode = e.Section?.Course?.Code,
                CourseName = e.Section?.Course?.Name,
                CreditHours = e.Section?.Course?.CreditHours ?? 3,
                SemesterId = e.Section?.SemesterId,
                SemesterName = e.Section?.Semester?.Name,
                Week7Score = r?.Week7Score,
                Week12Score = r?.Week12Score,
                PrefinalScore = r?.PrefinalScore,
                FinalScore = r?.FinalScore,
                TotalScore = r?.TotalScore,
                LetterGrade = r?.LetterGrade,
                GpaPoints = scale?.GpaPoints,
                Published = r != null && r.Published
            };
        }).ToList();

        var gpa = await _gradingService.CalculateCumulativeGpaAsync(student.Id);
        var standing = _gradingService.GetAcademicStanding(gpa);

        if (semesterId.HasValue)
        {
            return new
            {
                SemesterId = semesterId.Value,
                CumulativeGpa = gpa,
                AcademicStanding = standing,
                Results = resultList
            };
        }

        return new
        {
            CumulativeGpa = gpa,
            AcademicStanding = standing,
            Results = resultList
        };
    }

    public async Task<object> GetSectionResultsAsync(string instructorKeycloakId, Guid sectionId)
    {
        var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == instructorKeycloakId);
        if (instructor == null)
        {
            throw new UnauthorizedAccessException("Instructor record not found.");
        }

        var section = await _context.Sections.FindAsync(sectionId);
        if (section == null)
        {
            throw new KeyNotFoundException("Section not found.");
        }

        if (section.InstructorId != instructor.Id)
        {
            throw new UnauthorizedAccessException("You do not own this section.");
        }

        var results = await _context.Results
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Student)
            .Where(r => r.Enrollment!.SectionId == sectionId)
            .ToListAsync();

        var roster = results.Select(r => new
        {
            EnrollmentId = r.EnrollmentId,
            StudentName = r.Enrollment?.Student?.FullName,
            StudentNumber = r.Enrollment?.Student?.StudentNumber,
            Week7Score = r.Week7Score,
            Week12Score = r.Week12Score,
            PrefinalScore = r.PrefinalScore,
            FinalScore = r.FinalScore,
            TotalScore = r.TotalScore,
            LetterGrade = r.LetterGrade,
            Published = r.Published,
            IsManualOverride = r.IsManualOverride
        }).ToList();

        return roster;
    }

    public async Task UpdateResultAsync(string instructorKeycloakId, Guid enrollmentId, UpdateScoresRequest request)
    {
        var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == instructorKeycloakId);
        if (instructor == null)
        {
            throw new UnauthorizedAccessException("Instructor record not found.");
        }

        var result = await _context.Results
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Section)
            .FirstOrDefaultAsync(r => r.EnrollmentId == enrollmentId);

        if (result == null)
        {
            throw new KeyNotFoundException("Result record not found for the enrollment.");
        }

        if (result.Enrollment?.Section?.InstructorId != instructor.Id)
        {
            throw new UnauthorizedAccessException("You do not own this section enrollment.");
        }

        result.Week7Score = request.Week7Score;
        result.Week12Score = request.Week12Score;
        result.PrefinalScore = request.PrefinalScore;
        result.FinalScore = request.FinalScore;
        result.IsManualOverride = request.IsManualOverride;

        if (request.IsManualOverride)
        {
            result.LetterGrade = request.LetterGrade;
            // TotalScore is null or not updated here, optionally calculate if scores exist but LetterGrade is custom
            _context.Results.Update(result);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.Results.Update(result);
            await _context.SaveChangesAsync();

            // Recalculate using GradingService
            await _gradingService.RecalculateResultAsync(result.Id);
        }
    }

    public async Task PublishResultAsync(string userKeycloakId, string role, Guid enrollmentId)
    {
        var result = await _context.Results
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Section)
                    .ThenInclude(s => s!.Course)
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Student)
            .FirstOrDefaultAsync(r => r.EnrollmentId == enrollmentId);

        if (result == null)
        {
            throw new KeyNotFoundException("Result not found.");
        }

        if (role != "admin")
        {
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == userKeycloakId);
            if (instructor == null || result.Enrollment?.Section?.InstructorId != instructor.Id)
            {
                throw new UnauthorizedAccessException("You do not have permission to publish this result.");
            }
        }

        result.Published = true;
        _context.Results.Update(result);
        await _context.SaveChangesAsync();

        var studentId = result.Enrollment?.StudentId;
        var courseName = result.Enrollment?.Section?.Course?.Name ?? "Course";

        if (studentId.HasValue)
        {
            await _notificationService.SendPushAsync(
                studentId.Value,
                "result",
                "Grade Published",
                $"Your grade for {courseName} has been published"
            );
        }
    }

    public async Task PublishSectionResultsAsync(string instructorKeycloakId, Guid sectionId)
    {
        var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == instructorKeycloakId);
        if (instructor == null)
        {
            throw new UnauthorizedAccessException("Instructor record not found.");
        }

        var section = await _context.Sections.FindAsync(sectionId);
        if (section == null)
        {
            throw new KeyNotFoundException("Section not found.");
        }

        if (section.InstructorId != instructor.Id)
        {
            throw new UnauthorizedAccessException("You do not own this section.");
        }

        var results = await _context.Results
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Student)
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Section)
                    .ThenInclude(s => s!.Course)
            .Where(r => r.Enrollment!.SectionId == sectionId && !r.Published)
            .ToListAsync();

        foreach (var result in results)
        {
            result.Published = true;
            _context.Results.Update(result);

            var studentId = result.Enrollment?.StudentId;
            var courseName = result.Enrollment?.Section?.Course?.Name ?? "Course";

            if (studentId.HasValue)
            {
                await _notificationService.SendPushAsync(
                    studentId.Value,
                    "result",
                    "Grade Published",
                    $"Your grade for {courseName} has been published"
                );
            }
        }

        await _context.SaveChangesAsync();
    }
}
