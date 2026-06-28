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
    private readonly IFaceRecognitionClient _faceRecognitionClient;
    private readonly INotificationService _notificationService;

    // Thread-safe in-memory store for failed attempts: (StudentId, SessionId) -> List of timestamps of failures
    private static readonly ConcurrentDictionary<(Guid StudentId, Guid SessionId), List<DateTime>> _failedAttempts = new();

    public AttendanceService(
        AppDbContext context, 
        IGeofenceService geofenceService,
        IFaceRecognitionClient faceRecognitionClient,
        INotificationService notificationService)
    {
        _context = context;
        _geofenceService = geofenceService;
        _faceRecognitionClient = faceRecognitionClient;
        _notificationService = notificationService;
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

        if (request.Method.Equals("face", StringComparison.OrdinalIgnoreCase))
        {
            var hasFaceStudent = await _context.Enrollments.AnyAsync(e => e.SectionId == request.SectionId && e.FaceAttendanceEnabled && !e.IsWithdrawn);
            if (!hasFaceStudent)
            {
                throw new InvalidOperationException("Face attendance is not enabled for any student enrolled in this section.");
            }
            currentCode = "face";
            codeExpiresAt = endTime;
        }
        else if (request.Method.Equals("pin", StringComparison.OrdinalIgnoreCase))
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
            RadiusMeters = request.RadiusMeters,
            Week = request.Week
        };

        await _context.AttendanceSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Default previous weeks (1 to session.Week - 1) to absent if no sessions exist for them
        var enrolledStudents = await _context.Enrollments
            .Where(e => e.SectionId == session.SectionId && !e.IsWithdrawn)
            .ToListAsync();

        for (int w = 1; w < session.Week; w++)
        {
            var sessionExists = await _context.AttendanceSessions
                .AnyAsync(s => s.SectionId == session.SectionId && s.Week == w);

            if (!sessionExists)
            {
                // Create a dummy closed session for week w
                var dummySession = new AttendanceSession
                {
                    Id = Guid.NewGuid(),
                    SectionId = session.SectionId,
                    InstructorId = instructor.Id,
                    StartTime = DateTime.UtcNow.AddDays(-7 * (session.Week - w)),
                    EndTime = DateTime.UtcNow.AddDays(-7 * (session.Week - w)).AddMinutes(90),
                    Method = "system_default",
                    CurrentCode = "system",
                    CodeExpiresAt = DateTime.UtcNow,
                    Lat = session.Lat,
                    Lng = session.Lng,
                    RadiusMeters = session.RadiusMeters,
                    Week = w
                };
                await _context.AttendanceSessions.AddAsync(dummySession);
                await _context.SaveChangesAsync();

                // Create absent records for all enrolled students
                foreach (var enrollment in enrolledStudents)
                {
                    var record = new AttendanceRecord
                    {
                        Id = Guid.NewGuid(),
                        SessionId = dummySession.Id,
                        StudentId = enrollment.StudentId,
                        CheckedInAt = dummySession.StartTime,
                        Status = "absent",
                        Method = "system_default"
                    };
                    await _context.AttendanceRecords.AddAsync(record);
                }
                await _context.SaveChangesAsync();
            }
        }

        // Run absence escalation for every enrolled student in this section up to the current session's week
        foreach (var enrollment in enrolledStudents)
        {
            await RecomputeAbsenceEscalationAsync(enrollment.StudentId, session.SectionId, session.Week);
        }

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

        // Enrolled students
        var enrolledStudents = await _context.Enrollments
            .Where(e => e.SectionId == session.SectionId && !e.IsWithdrawn)
            .ToListAsync();

        // 1. Default previous weeks (1 to session.Week - 1) to absent if no sessions exist for them
        for (int w = 1; w < session.Week; w++)
        {
            var sessionExists = await _context.AttendanceSessions
                .AnyAsync(s => s.SectionId == session.SectionId && s.Week == w);

            if (!sessionExists)
            {
                // Create a dummy closed session for week w
                var dummySession = new AttendanceSession
                {
                    Id = Guid.NewGuid(),
                    SectionId = session.SectionId,
                    InstructorId = instructor.Id,
                    StartTime = DateTime.UtcNow.AddDays(-7 * (session.Week - w)),
                    EndTime = DateTime.UtcNow.AddDays(-7 * (session.Week - w)).AddMinutes(90),
                    Method = "system_default",
                    CurrentCode = "system",
                    CodeExpiresAt = DateTime.UtcNow,
                    Lat = session.Lat,
                    Lng = session.Lng,
                    RadiusMeters = session.RadiusMeters,
                    Week = w
                };
                await _context.AttendanceSessions.AddAsync(dummySession);
                await _context.SaveChangesAsync();

                // Create absent records for all enrolled students
                foreach (var enrollment in enrolledStudents)
                {
                    var record = new AttendanceRecord
                    {
                        Id = Guid.NewGuid(),
                        SessionId = dummySession.Id,
                        StudentId = enrollment.StudentId,
                        CheckedInAt = dummySession.StartTime,
                        Status = "absent",
                        Method = "system_default"
                    };
                    await _context.AttendanceRecords.AddAsync(record);
                }
                await _context.SaveChangesAsync();
            }
        }

        // 2. Mark absent for all enrolled students who did not check in
        var checkedInStudentIds = await _context.AttendanceRecords
            .Where(r => r.SessionId == session.Id && r.Status == "present")
            .Select(r => r.StudentId)
            .ToListAsync();

        foreach (var enrollment in enrolledStudents)
        {
            if (!checkedInStudentIds.Contains(enrollment.StudentId))
            {
                var existingRecord = await _context.AttendanceRecords
                    .FirstOrDefaultAsync(r => r.SessionId == session.Id && r.StudentId == enrollment.StudentId);
                
                if (existingRecord == null)
                {
                    var record = new AttendanceRecord
                    {
                        Id = Guid.NewGuid(),
                        SessionId = session.Id,
                        StudentId = enrollment.StudentId,
                        CheckedInAt = DateTime.UtcNow,
                        Status = "absent",
                        Method = session.Method
                    };
                    await _context.AttendanceRecords.AddAsync(record);
                }
                else if (existingRecord.Status != "present")
                {
                    existingRecord.Status = "absent";
                    _context.AttendanceRecords.Update(existingRecord);
                }
            }
        }
        await _context.SaveChangesAsync();

        // 3. Run absence escalation for every enrolled student in this section up to the current session's week (plus 1 to include the closed week)
        foreach (var enrollment in enrolledStudents)
        {
            await RecomputeAbsenceEscalationAsync(enrollment.StudentId, session.SectionId, session.Week + 1);
        }
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
                    r.Status,
                    r.Method,
                    Week = r.Session!.Week
                })
                .ToListAsync();

            var presentCount = records.Count(r => r.Status.Equals("present", StringComparison.OrdinalIgnoreCase));
            double percentage = totalSessions == 0 ? 100.0 : (double)presentCount / totalSessions * 100.0;

            var absentCount = records.Count(r => r.Status.Equals("absent", StringComparison.OrdinalIgnoreCase));
            bool absenceWarning = absentCount >= 2;
            bool withdrawalPending = enrollment.WithdrawalPending;
            bool isWithdrawn = enrollment.IsWithdrawn;
            string? autoWithdrawnMessage = isWithdrawn
                ? $"{student.FullName} was automatically withdrawn after 4 absences."
                : null;

            studentSummaries.Add(new
            {
                StudentId = student.Id,
                StudentNumber = student.StudentNumber,
                FullName = student.FullName,
                AttendanceRecords = records,
                OverallAttendancePercentage = percentage,
                AbsenceWarning = absenceWarning,
                WithdrawalPending = withdrawalPending,
                IsWithdrawn = isWithdrawn,
                AutoWithdrawnMessage = autoWithdrawnMessage,
                EnrollmentId = enrollment.Id
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

    private async Task RecomputeAbsenceEscalationAsync(Guid studentId, Guid sectionId, int currentWeek)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Section)
                .ThenInclude(s => s!.Course)
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.SectionId == sectionId);
        
        if (enrollment == null || enrollment.IsWithdrawn) return;

        // Count absences strictly before the current week
        var absentCount = await _context.AttendanceRecords
            .CountAsync(r => r.StudentId == studentId && 
                             r.Session!.SectionId == sectionId && 
                             r.Status == "absent" && 
                             r.Session.Week < currentWeek);

        if (absentCount == 3 && !enrollment.WithdrawalPending)
        {
            enrollment.WithdrawalPending = true;
            _context.Enrollments.Update(enrollment);
            await _context.SaveChangesAsync();
        }
        else if (absentCount >= 4)
        {
            enrollment.IsWithdrawn = true;
            enrollment.WithdrawnAt = DateTime.UtcNow;
            enrollment.WithdrawalPending = false;
            _context.Enrollments.Update(enrollment);
            await _context.SaveChangesAsync();

            // Send notification
            await _notificationService.SendPushAsync(
                studentId,
                "auto_withdrawal",
                "Automatic Course Withdrawal",
                $"You have been automatically withdrawn from {enrollment.Section?.Course?.Name ?? "the course"} after 4 absences."
            );
        }
    }

    public async Task<List<FaceCheckInResult>> FaceCheckInAsync(string instructorKeycloakId, Guid sessionId, string base64Image)
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

        if (DateTime.UtcNow >= session.EndTime)
        {
            throw new InvalidOperationException("Session is closed.");
        }

        // Call face recognition microservice
        var response = await _faceRecognitionClient.RecognizeAsync(base64Image);
        var results = new List<FaceCheckInResult>();

        if (response == null || response.Matches == null || response.Matches.Count == 0)
        {
            results.Add(new FaceCheckInResult { Status = "no_face" });
            return results;
        }

        foreach (var match in response.Matches)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.FaceEncodingKey == match.StudentKey);
            if (student == null)
            {
                results.Add(new FaceCheckInResult { Status = "not_registered", StudentKey = match.StudentKey });
                continue;
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.SectionId == session.SectionId);

            if (enrollment == null || !enrollment.FaceAttendanceEnabled || enrollment.IsWithdrawn)
            {
                results.Add(new FaceCheckInResult { Status = "not_registered", StudentKey = match.StudentKey });
                continue;
            }

            // Create attendance record
            var alreadyCheckedIn = await _context.AttendanceRecords
                .AnyAsync(r => r.SessionId == session.Id && r.StudentId == student.Id);

            if (!alreadyCheckedIn)
            {
                var record = new AttendanceRecord
                {
                    Id = Guid.NewGuid(),
                    SessionId = session.Id,
                    StudentId = student.Id,
                    CheckedInAt = DateTime.UtcNow,
                    Status = "present",
                    Method = "face",
                    Confidence = match.Confidence
                };
                await _context.AttendanceRecords.AddAsync(record);
                await _context.SaveChangesAsync();

                results.Add(new FaceCheckInResult
                {
                    Status = "success",
                    StudentKey = match.StudentKey,
                    Name = student.FullName,
                    Confidence = match.Confidence
                });
            }
            else
            {
                results.Add(new FaceCheckInResult
                {
                    Status = "already_checked_in",
                    StudentKey = match.StudentKey,
                    Name = student.FullName,
                    Confidence = match.Confidence
                });
            }
        }

        return results;
    }

    public async Task<object> FaceStudentCheckInAsync(string studentKeycloakId, Guid sessionId, string base64Image)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.KeycloakId == studentKeycloakId);
        if (student == null) throw new UnauthorizedAccessException("Student record not found.");

        var session = await _context.AttendanceSessions.FindAsync(sessionId);
        if (session == null) throw new KeyNotFoundException("Session not found.");

        if (DateTime.UtcNow >= session.EndTime)
            throw new InvalidOperationException("Session is closed.");

        if (!session.Method.Equals("face", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This session does not use face recognition. Please use QR or PIN.");

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.SectionId == session.SectionId && !e.IsWithdrawn);
        if (enrollment == null)
            throw new InvalidOperationException("You are not enrolled in this course.");
        if (!enrollment.FaceAttendanceEnabled)
            throw new InvalidOperationException("Face attendance is not enabled for your account.");

        var alreadyCheckedIn = await _context.AttendanceRecords
            .AnyAsync(r => r.SessionId == session.Id && r.StudentId == student.Id && r.Status == "present");
        if (alreadyCheckedIn)
            throw new InvalidOperationException("You have already checked in for this session.");

        var response = await _faceRecognitionClient.RecognizeAsync(base64Image);

        if (response == null || response.Matches == null || response.Matches.Count == 0)
            return new { status = "no_face", message = "No face detected. Please ensure your face is clearly visible and try again." };

        var match = response.Matches.FirstOrDefault(m => m.StudentKey == student.FaceEncodingKey);
        if (match == null)
            return new { status = "not_recognized", message = "Face not recognized. Please try again." };

        var record = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            StudentId = student.Id,
            CheckedInAt = DateTime.UtcNow,
            Status = "present",
            Method = "face",
            Confidence = match.Confidence
        };
        await _context.AttendanceRecords.AddAsync(record);
        await _context.SaveChangesAsync();

        return new { status = "success", message = "Face recognized — Checked in!", confidence = match.Confidence };
    }

    public async Task DecidedWithdrawalAsync(string instructorKeycloakId, Guid sectionId, Guid enrollmentId, bool approve)
    {
        var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == instructorKeycloakId);
        if (instructor == null)
        {
            throw new UnauthorizedAccessException("Instructor record not found.");
        }

        var section = await _context.Sections.FindAsync(sectionId);
        if (section == null || section.InstructorId != instructor.Id)
        {
            throw new UnauthorizedAccessException("You do not own this section.");
        }

        var enrollment = await _context.Enrollments
            .Include(e => e.Section)
                .ThenInclude(s => s!.Course)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.SectionId == sectionId);

        if (enrollment == null)
        {
            throw new KeyNotFoundException("Enrollment not found.");
        }

        if (approve)
        {
            enrollment.IsWithdrawn = true;
            enrollment.WithdrawnAt = DateTime.UtcNow;
            enrollment.WithdrawalPending = false;
            _context.Enrollments.Update(enrollment);
            await _context.SaveChangesAsync();

            // Send notification
            await _notificationService.SendPushAsync(
                enrollment.StudentId,
                "withdrawal_approved",
                "Course Withdrawal",
                $"You have been withdrawn from {enrollment.Section?.Course?.Name ?? "the course"} by the instructor."
            );
        }
        else
        {
            enrollment.WithdrawalPending = false;
            _context.Enrollments.Update(enrollment);
            await _context.SaveChangesAsync();
        }
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
