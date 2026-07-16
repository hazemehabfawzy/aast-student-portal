using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.DTOs.Responses;
using StudentPortal.Api.Models.Entities;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Services.Implementations;

public class ScheduleConflictException : Exception
{
    public ConflictResponse ConflictDetails { get; }

    public ScheduleConflictException(ConflictResponse details)
    {
        ConflictDetails = details;
    }
}

public class RegistrationService : IRegistrationService
{
    private readonly AppDbContext _context;

    public class ScheduleItem
    {
        public string Day { get; set; } = string.Empty;
        public string? Time { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
    }

    public RegistrationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetAvailableSectionsAsync(string studentKeycloakId, Guid semesterId)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.KeycloakId == studentKeycloakId);
        if (student == null)
        {
            throw new UnauthorizedAccessException("Student record not found.");
        }

        // Check if there is an open registration period for the semester
        var regPeriod = await _context.RegistrationPeriods
            .FirstOrDefaultAsync(rp => rp.SemesterId == semesterId && rp.IsOpen && 
                                       DateTime.UtcNow >= rp.StartDate && DateTime.UtcNow <= rp.EndDate);

        if (regPeriod == null)
        {
            return new List<object>(); // or throw RegistrationNotOpenException? The endpoint spec says: "Returns available sections for the semester where a RegistrationPeriod exists and IsOpen=true"
        }

        var sections = await _context.Sections
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .Where(s => s.SemesterId == semesterId && s.IsActive)
            .ToListAsync();

        var list = new List<object>();

        var studentEnrollments = await _context.Enrollments
            .Where(e => e.StudentId == student.Id && e.Section!.SemesterId == semesterId)
            .ToListAsync();

        foreach (var s in sections)
        {
            var enrolledCount = await _context.Enrollments.CountAsync(e => e.SectionId == s.Id);
            var seatsLeft = s.Capacity - enrolledCount;
            var enrollment = studentEnrollments.FirstOrDefault(e => e.SectionId == s.Id);

            list.Add(new
            {
                SectionId = s.Id,
                CourseCode = s.Course?.Code,
                CourseName = s.Course?.Name,
                InstructorName = s.Instructor?.FullName,
                Schedule = s.ScheduleJson,
                ScheduleJson = s.ScheduleJson,
                Capacity = s.Capacity,
                EnrolledCount = enrolledCount,
                SeatsLeft = seatsLeft,
                PrerequisiteCode = s.Course?.PrerequisiteCode,
                IsEnrolled = enrollment != null,
                EnrollmentId = enrollment?.Id
            });
        }

        return list;
    }

    public async Task<object> EnrollAsync(string studentKeycloakId, Guid sectionId)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.KeycloakId == studentKeycloakId);
        if (student == null)
        {
            throw new UnauthorizedAccessException("Student record not found.");
        }

        var section = await _context.Sections
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        if (section == null)
        {
            throw new KeyNotFoundException("Section not found.");
        }

        // 1. RegistrationPeriod check
        var now = DateTime.UtcNow;
        var regPeriod = await _context.RegistrationPeriods
            .FirstOrDefaultAsync(rp => rp.SemesterId == section.SemesterId && rp.IsOpen &&
                                       now >= rp.StartDate && now <= rp.EndDate);

        if (regPeriod == null)
        {
            throw new InvalidOperationException("Registration is not currently open");
        }

        // 2. Section active check
        if (!section.IsActive)
        {
            throw new InvalidOperationException("Section is not available");
        }

        // 2b. Prerequisite check
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

        if (!PrerequisiteHelper.IsPrerequisiteMet(section.Course?.PrerequisiteCode, passedCourseCodes))
        {
            throw new InvalidOperationException(
                $"Prerequisite not met: {section.Course?.PrerequisiteCode}");
        }

        // 3. Capacity check
        var enrolledCount = await _context.Enrollments.CountAsync(e => e.SectionId == sectionId);
        if (enrolledCount >= section.Capacity)
        {
            throw new InvalidOperationException("Section is full");
        }

        // 4. Duplicate enrollment check
        var alreadyEnrolled = await _context.Enrollments.AnyAsync(e => e.StudentId == student.Id && e.SectionId == sectionId);
        if (alreadyEnrolled)
        {
            throw new InvalidOperationException("Already enrolled");
        }

        // 5. Schedule Conflict Check
        var currentEnrollments = await _context.Enrollments
            .Include(e => e.Section)
                .ThenInclude(s => s!.Course)
            .Where(e => e.StudentId == student.Id && e.Section!.SemesterId == section.SemesterId)
            .ToListAsync();

        var newSchedule = ParseSchedule(section.ScheduleJson);
        var conflictingSectionName = "";
        var conflictDetected = false;

        foreach (var existingEnv in currentEnrollments)
        {
            var existingSchedule = ParseSchedule(existingEnv.Section!.ScheduleJson);
            if (HasScheduleOverlap(newSchedule, existingSchedule))
            {
                conflictDetected = true;
                conflictingSectionName = $"{existingEnv.Section.Course?.Code} – {FormatScheduleDisplay(existingSchedule)}";
                break;
            }
        }

        if (conflictDetected)
        {
            // Find Alternatives
            var allSectionsOfSameCourse = await _context.Sections
                .Include(s => s.Course)
                .Where(s => s.CourseId == section.CourseId && s.Id != sectionId && s.IsActive && s.SemesterId == section.SemesterId)
                .ToListAsync();

            var alternatives = new List<AlternativeSectionDto>();

            foreach (var altSection in allSectionsOfSameCourse)
            {
                var altEnrolled = await _context.Enrollments.CountAsync(e => e.SectionId == altSection.Id);
                var altSeats = altSection.Capacity - altEnrolled;

                if (altSeats > 0)
                {
                    var altSchedule = ParseSchedule(altSection.ScheduleJson);
                    // Check conflict against the student's existing schedule
                    bool altConflict = false;
                    foreach (var existingEnv in currentEnrollments)
                    {
                        var existingSchedule = ParseSchedule(existingEnv.Section!.ScheduleJson);
                        if (HasScheduleOverlap(altSchedule, existingSchedule))
                        {
                            altConflict = true;
                            break;
                        }
                    }

                    if (!altConflict)
                    {
                        alternatives.Add(new AlternativeSectionDto
                        {
                            SectionId = altSection.Id,
                            CourseCode = altSection.Course?.Code ?? string.Empty,
                            Schedule = FormatScheduleDisplay(altSchedule),
                            SeatsLeft = altSeats
                        });
                    }
                }
            }

            var conflictResponse = new ConflictResponse
            {
                Conflict = true,
                ConflictingWith = conflictingSectionName,
                Alternatives = alternatives
            };

            throw new ScheduleConflictException(conflictResponse);
        }

        // On success: Create Enrollment
        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            SectionId = sectionId,
            WarningSent = false
        };

        // Create an empty Result record for this enrollment so scores can be added later
        var result = new Result
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollment.Id,
            Published = false,
            IsManualOverride = false
        };

        await _context.Enrollments.AddAsync(enrollment);
        await _context.Results.AddAsync(result);
        await _context.SaveChangesAsync();

        return enrollment;
    }

    public async Task DropEnrollmentAsync(string studentKeycloakId, Guid enrollmentId)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.KeycloakId == studentKeycloakId);
        if (student == null)
        {
            throw new UnauthorizedAccessException("Student record not found.");
        }

        var enrollment = await _context.Enrollments
            .Include(e => e.Section)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment == null)
        {
            throw new KeyNotFoundException("Enrollment not found.");
        }

        if (enrollment.StudentId != student.Id)
        {
            throw new UnauthorizedAccessException("You do not own this enrollment.");
        }

        // Validate registration period is open
        var now = DateTime.UtcNow;
        var regPeriod = await _context.RegistrationPeriods
            .FirstOrDefaultAsync(rp => rp.SemesterId == enrollment.Section!.SemesterId && rp.IsOpen &&
                                       now >= rp.StartDate && now <= rp.EndDate);

        if (regPeriod == null)
        {
            throw new InvalidOperationException("Registration period is closed. Contact admin to request a withdrawal.");
        }

        // Delete associated Results and AttendanceRecords if any exist
        var results = await _context.Results.Where(r => r.EnrollmentId == enrollmentId).ToListAsync();
        _context.Results.RemoveRange(results);

        _context.Enrollments.Remove(enrollment);
        await _context.SaveChangesAsync();
    }

    public async Task<object> GetStudentScheduleAsync(string studentKeycloakId)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.KeycloakId == studentKeycloakId);
        if (student == null)
        {
            throw new UnauthorizedAccessException("Student record not found.");
        }

        var enrollments = await _context.Enrollments
            .Include(e => e.Section)
                .ThenInclude(s => s!.Course)
            .Include(e => e.Section)
                .ThenInclude(s => s!.Instructor)
            .Where(e => e.StudentId == student.Id && e.Section!.IsActive)
            .ToListAsync();

        return enrollments.Select(e => new
        {
            sectionId = e.SectionId,
            courseCode = e.Section?.Course?.Code,
            courseName = e.Section?.Course?.Name,
            instructorName = e.Section?.Instructor?.FullName,
            scheduleJson = e.Section?.ScheduleJson
        }).ToList();
    }

    private static List<ScheduleItem> ParseSchedule(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<ScheduleItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ScheduleItem>();
        }
        catch
        {
            return new List<ScheduleItem>();
        }
    }

    private static bool HasScheduleOverlap(List<ScheduleItem> list1, List<ScheduleItem> list2)
    {
        foreach (var item1 in list1)
        {
            foreach (var item2 in list2)
            {
                if (item1.Day.Equals(item2.Day, StringComparison.OrdinalIgnoreCase))
                {
                    int start1 = 0, end1 = 0, start2 = 0, end2 = 0;

                    // Parse item1
                    if (!string.IsNullOrEmpty(item1.Time))
                    {
                        var parts = item1.Time.Split('-');
                        if (parts.Length == 2)
                        {
                            start1 = ParseTimeToMinutes(parts[0]);
                            end1 = ParseTimeToMinutes(parts[1]);
                        }
                    }
                    else if (!string.IsNullOrEmpty(item1.StartTime) && !string.IsNullOrEmpty(item1.EndTime))
                    {
                        start1 = ParseTimeToMinutes(item1.StartTime);
                        end1 = ParseTimeToMinutes(item1.EndTime);
                    }

                    // Parse item2
                    if (!string.IsNullOrEmpty(item2.Time))
                    {
                        var parts = item2.Time.Split('-');
                        if (parts.Length == 2)
                        {
                            start2 = ParseTimeToMinutes(parts[0]);
                            end2 = ParseTimeToMinutes(parts[1]);
                        }
                    }
                    else if (!string.IsNullOrEmpty(item2.StartTime) && !string.IsNullOrEmpty(item2.EndTime))
                    {
                        start2 = ParseTimeToMinutes(item2.StartTime);
                        end2 = ParseTimeToMinutes(item2.EndTime);
                    }

                    // Check overlap
                    if (start1 < end2 && start2 < end1)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static int ParseTimeToMinutes(string timeStr)
    {
        var parts = timeStr.Trim().Split(':');
        if (parts.Length == 2 && int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes))
        {
            return hours * 60 + minutes;
        }
        return 0;
    }

    private static string FormatScheduleDisplay(List<ScheduleItem> schedule)
    {
        return string.Join(", ", schedule.Select(item =>
        {
            var timeRange = !string.IsNullOrEmpty(item.Time) 
                ? item.Time 
                : $"{item.StartTime}-{item.EndTime}";
            return $"{item.Day.Substring(0, Math.Min(3, item.Day.Length))} {timeRange}";
        }));
    }
}
