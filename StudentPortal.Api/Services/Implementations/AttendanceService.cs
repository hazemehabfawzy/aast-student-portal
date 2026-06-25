using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.DTOs.Requests;
using StudentPortal.Api.Models.DTOs.Responses;
using StudentPortal.Api.Models.Entities;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Services.Implementations;

public class RateLimitException : Exception
{
    public RateLimitException(string message) : base(message) { }
}

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _context;
    private readonly IGeofenceService _geofenceService;

    // Thread-safe in-memory store for failed attempts: (StudentId, SessionId) -> List of timestamps of failures
    private static readonly ConcurrentDictionary<(Guid StudentId, Guid SessionId), List<DateTime>> _failedAttempts = new();

    public AttendanceService(AppDbContext context, IGeofenceService geofenceService)
    {
        _context = context;
        _geofenceService = geofenceService;
    }

    public async Task<SessionResponse> CreateSessionAsync(string instructorKeycloakId, CreateSessionRequest request)
    {
        var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == instructorKeycloakId);
        if (instructor == null)
        {
            throw new UnauthorizedAccessException("Instructor record not found.");
        }

        var section = await _context.Sections.FindAsync(request.SectionId);
        if (section == null)
        {
            throw new KeyNotFoundException("Section not found.");
        }

        if (section.InstructorId != instructor.Id)
        {
            throw new UnauthorizedAccessException("You do not own this section.");
        }

        var duration = request.DurationMinutes ?? 90;
        var now = DateTime.UtcNow;
        var endTime = now.AddMinutes(duration);

        string currentCode;
        DateTime codeExpiresAt;

        if (request.Method.Equals("pin", StringComparison.OrdinalIgnoreCase))
        {
            // PIN is a random 6-digit string valid for the whole session
            var random = new Random();
            currentCode = random.Next(100000, 999999).ToString();
            codeExpiresAt = endTime;
        }
        else
        {
            // QR generates a GUID valid for 30 seconds
            currentCode = Guid.NewGuid().ToString();
            codeExpiresAt = now.AddSeconds(30);
        }

        var session = new AttendanceSession
        {
            Id = Guid.NewGuid(),
            SectionId = request.SectionId,
            InstructorId = instructor.Id,
            StartTime = now,
            EndTime = endTime,
            Method = request.Method.ToLower(),
            CurrentCode = currentCode,
            CodeExpiresAt = codeExpiresAt,
            Lat = request.Lat,
            Lng = request.Lng,
            RadiusMeters = request.RadiusMeters
        };

        await _context.AttendanceSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        return new SessionResponse
        {
            SessionId = session.Id,
            Method = session.Method,
            CurrentCode = session.CurrentCode,
            ExpiresAt = session.CodeExpiresAt
        };
    }

    public async Task<string> RotateSessionCodeAsync(string instructorKeycloakId, Guid sessionId)
    {
        var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == instructorKeycloakId);
        if (instructor == null)
        {
            throw new UnauthorizedAccessException("Instructor record not found.");
        }

        var session = await _context.AttendanceSessions.FindAsync(sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        var section = await _context.Sections.FindAsync(session.SectionId);
        if (section == null || section.InstructorId != instructor.Id)
        {
            throw new UnauthorizedAccessException("You do not own this session.");
        }

        if (session.Method.Equals("pin", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("PIN sessions do not support code rotation.");
        }

        var now = DateTime.UtcNow;
        session.CurrentCode = Guid.NewGuid().ToString();
        session.CodeExpiresAt = now.AddSeconds(30);

        _context.AttendanceSessions.Update(session);
        await _context.SaveChangesAsync();

        return session.CurrentCode;
    }

    public async Task CheckInAsync(string studentKeycloakId, CheckInRequest request)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.KeycloakId == studentKeycloakId);
        if (student == null)
        {
            throw new UnauthorizedAccessException("Student record not found.");
        }

        var session = await _context.AttendanceSessions.FindAsync(request.SessionId);
        if (session == null)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        // Check Rate Limit FIRST before other validations
        var rateLimitKey = (student.Id, session.Id);
        var attempts = _failedAttempts.GetOrAdd(rateLimitKey, _ => new List<DateTime>());
        lock (attempts)
        {
            attempts.RemoveAll(t => t < DateTime.UtcNow.AddMinutes(-5));
            if (attempts.Count >= 5)
            {
                throw new RateLimitException("Too many attempts");
            }
        }

        try
        {
            // 1. Session exists and is not closed
            if (DateTime.UtcNow >= session.EndTime)
            {
                throw new InvalidOperationException("Session is closed");
            }

            // 2. Code matches CurrentCode AND now < CodeExpiresAt
            if (session.CurrentCode != request.Code)
            {
                throw new InvalidOperationException("Invalid code");
            }
            if (DateTime.UtcNow >= session.CodeExpiresAt)
            {
                throw new InvalidOperationException("Code expired");
            }

            // 3. Student enrolled in the session's SectionId
            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.StudentId == student.Id && e.SectionId == session.SectionId);
            if (!isEnrolled)
            {
                throw new InvalidOperationException("Not enrolled in this course");
            }

            // 4. now is between session StartTime and EndTime
            if (DateTime.UtcNow < session.StartTime || DateTime.UtcNow > session.EndTime)
            {
                throw new InvalidOperationException("Outside session time window");
            }

            // 5. Geofence check
            var isWithinGeo = _geofenceService.IsWithinRadius(request.Lat, request.Lng, session.Lat, session.Lng, session.RadiusMeters);
            if (!isWithinGeo)
            {
                throw new InvalidOperationException("Too far from classroom location");
            }

            // 6. Already checked in
            var alreadyCheckedIn = await _context.AttendanceRecords.AnyAsync(r => r.SessionId == session.Id && r.StudentId == student.Id);
            if (alreadyCheckedIn)
            {
                throw new InvalidOperationException("Already checked in");
            }

            // On success: Create AttendanceRecord
            var record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                StudentId = student.Id,
                CheckedInAt = DateTime.UtcNow,
                Status = "present"
            };

            await _context.AttendanceRecords.AddAsync(record);
            await _context.SaveChangesAsync();

            // Clear failure attempts on successful check-in
            lock (attempts)
            {
                attempts.Clear();
            }
        }
        catch (Exception ex) when (!(ex is RateLimitException))
        {
            // Track failed check-in attempt
            lock (attempts)
            {
                attempts.Add(DateTime.UtcNow);
            }
            throw;
        }
    }

    public async Task CloseSessionAsync(string instructorKeycloakId, Guid sessionId)
    {
        var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == instructorKeycloakId);
        if (instructor == null)
        {
            throw new UnauthorizedAccessException("Instructor record not found.");
        }

        var session = await _context.AttendanceSessions.FindAsync(sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        var section = await _context.Sections.FindAsync(session.SectionId);
        if (section == null || section.InstructorId != instructor.Id)
        {
            throw new UnauthorizedAccessException("You do not own this session.");
        }

        session.EndTime = DateTime.UtcNow;
        _context.AttendanceSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public async Task<object> GetSectionAttendanceAsync(string instructorKeycloakId, Guid sectionId)
    {
        var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == instructorKeycloakId);
        if (instructor == null)
        {
            throw new UnauthorizedAccessException("Instructor record not found.");
        }

        var section = await _context.Sections
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        if (section == null)
        {
            throw new KeyNotFoundException("Section not found.");
        }

        if (section.InstructorId != instructor.Id)
        {
            throw new UnauthorizedAccessException("You do not own this section.");
        }

        // Total sessions in this section
        var totalSessions = await _context.AttendanceSessions.CountAsync(s => s.SectionId == sectionId);

        // Enrolled students
        var enrollments = await _context.Enrollments
            .Include(e => e.Student)
            .Where(e => e.SectionId == sectionId)
            .ToListAsync();

        var studentSummaries = new List<object>();

        foreach (var enrollment in enrollments)
        {
            var student = enrollment.Student;
            if (student == null) continue;

            // Attendance records for this student in this section
            var records = await _context.AttendanceRecords
                .Where(r => r.StudentId == student.Id && r.Session!.SectionId == sectionId)
                .Select(r => new
                {
                    r.SessionId,
                    r.CheckedInAt,
                    r.Status
                })
                .ToListAsync();

            var presentCount = records.Count(r => r.Status.Equals("present", StringComparison.OrdinalIgnoreCase));
            double percentage = totalSessions == 0 ? 100.0 : (double)presentCount / totalSessions * 100.0;

            studentSummaries.Add(new
            {
                StudentId = student.Id,
                StudentNumber = student.StudentNumber,
                FullName = student.FullName,
                AttendanceRecords = records,
                OverallAttendancePercentage = percentage
            });
        }

        return new
        {
            SectionId = sectionId,
            CourseCode = section.Course?.Code,
            CourseName = section.Course?.Name,
            TotalSessions = totalSessions,
            Students = studentSummaries
        };
    }

    public async Task<object> GetStudentMeAttendanceAsync(string studentKeycloakId)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.KeycloakId == studentKeycloakId);
        if (student == null)
        {
            throw new UnauthorizedAccessException("Student record not found.");
        }

        var enrollments = await _context.Enrollments
            .Include(e => e.Section)
                .ThenInclude(s => s!.Course)
            .Where(e => e.StudentId == student.Id)
            .ToListAsync();

        var summaries = new List<object>();

        foreach (var enrollment in enrollments)
        {
            var section = enrollment.Section;
            if (section == null) continue;

            var totalSessions = await _context.AttendanceSessions.CountAsync(s => s.SectionId == section.Id);
            var presentCount = await _context.AttendanceRecords
                .CountAsync(r => r.StudentId == student.Id && r.Session!.SectionId == section.Id && r.Status == "present");

            double percentage = totalSessions == 0 ? 100.0 : (double)presentCount / totalSessions * 100.0;

            summaries.Add(new
            {
                SectionId = section.Id,
                CourseCode = section.Course?.Code,
                CourseName = section.Course?.Name,
                PresentCount = presentCount,
                TotalSessions = totalSessions,
                AttendancePercentage = percentage
            });
        }

        return summaries;
    }
}
