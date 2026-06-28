using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models;
using StudentPortal.Api.Repositories;

namespace StudentPortal.Api.Services;

public class InstructorService : IInstructorService
{
    private readonly StudentPortalDbContext _context;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IGradeRepository _gradeRepository;

    public InstructorService(
        StudentPortalDbContext context,
        IAttendanceRepository attendanceRepository,
        IGradeRepository gradeRepository)
    {
        _context = context;
        _attendanceRepository = attendanceRepository;
        _gradeRepository = gradeRepository;
    }

    public async Task<IEnumerable<Section>> GetSectionsAsync(Guid instructorId)
    {
        return await _context.Sections
            .Include(s => s.Course)
            .Where(s => s.InstructorId == instructorId)
            .ToListAsync();
    }

    public async Task<bool> IsSectionOwnerAsync(int sectionId, Guid instructorId)
    {
        return await _context.Sections
            .AnyAsync(s => s.Id == sectionId && s.InstructorId == instructorId);
    }

    public async Task<AttendanceSession> CreateAttendanceSessionAsync(int sectionId, AttendanceSessionCreateDto dto)
    {
        // 1. Create the session
        var secretToken = new Random().Next(1000, 9999).ToString();
        var session = new AttendanceSession
        {
            SectionId = sectionId,
            SessionDate = DateTime.UtcNow,
            SecretToken = secretToken,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(dto.ExpirationMinutes)
        };

        await _attendanceRepository.AddSessionAsync(session);

        // 2. Fetch all students enrolled in the section
        var enrolledStudents = await _context.Enrollments
            .Where(e => e.SectionId == sectionId)
            .Select(e => e.StudentId)
            .ToListAsync();

        // 3. Pre-populate attendance records as "Absent"
        foreach (var studentId in enrolledStudents)
        {
            var record = new AttendanceRecord
            {
                SessionId = session.Id,
                StudentId = studentId,
                Status = "Absent"
            };
            await _attendanceRepository.AddRecordAsync(record);
        }

        return session;
    }

    public async Task<IEnumerable<AttendanceSession>> GetAttendanceSessionsAsync(int sectionId)
    {
        return await _attendanceRepository.GetSessionsBySectionIdAsync(sectionId);
    }

    public async Task<bool> ToggleSessionStatusAsync(int sessionId, bool isActive)
    {
        var session = await _attendanceRepository.GetSessionByIdAsync(sessionId);
        if (session == null) return false;

        session.IsActive = isActive;
        await _attendanceRepository.UpdateSessionAsync(session);
        return true;
    }

    public async Task<Grade> SubmitGradeAsync(GradeSubmitDto dto)
    {
        var grade = await _gradeRepository.GetGradeAsync(dto.StudentId, dto.SectionId, dto.ComponentName);
        if (grade == null)
        {
            grade = new Grade
            {
                StudentId = dto.StudentId,
                SectionId = dto.SectionId,
                ComponentName = dto.ComponentName,
                Score = dto.Score,
                MaxScore = dto.MaxScore
            };
            await _gradeRepository.AddGradeAsync(grade);
        }
        else
        {
            grade.Score = dto.Score;
            grade.MaxScore = dto.MaxScore;
            await _gradeRepository.UpdateGradeAsync(grade);
        }

        return grade;
    }
}
