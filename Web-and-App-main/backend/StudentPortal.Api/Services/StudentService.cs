using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models;

namespace StudentPortal.Api.Services;

public class StudentService : IStudentService
{
    private readonly StudentPortalDbContext _context;

    public StudentService(StudentPortalDbContext context)
    {
        _context = context;
    }

    public async Task<StudentProfileDto?> GetProfileAsync(Guid studentId)
    {
        var student = await _context.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null || student.User == null)
        {
            return null;
        }

        return new StudentProfileDto
        {
            Id = student.Id,
            FullName = student.User.FullName,
            Email = student.User.Email,
            RegistrationNumber = student.RegistrationNumber,
            Gpa = student.Gpa
        };
    }

    public async Task<IEnumerable<StudentScheduleItemDto>> GetScheduleAsync(Guid studentId)
    {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Section)
            .ThenInclude(s => s!.Course)
            .Select(e => new StudentScheduleItemDto
            {
                SectionId = e.SectionId,
                SectionName = e.Section!.Name,
                CourseCode = e.Section.Course!.Code,
                CourseName = e.Section.Course!.Name,
                Room = e.Section.Room,
                DayOfWeek = e.Section.DayOfWeek,
                StartTime = e.Section.StartTime,
                EndTime = e.Section.EndTime
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<StudentGradeDto>> GetGradesAsync(Guid studentId)
    {
        return await _context.Grades
            .Where(g => g.StudentId == studentId)
            .Include(g => g.Section)
            .ThenInclude(s => s!.Course)
            .Select(g => new StudentGradeDto
            {
                CourseCode = g.Section!.Course!.Code,
                CourseName = g.Section!.Course!.Name,
                ComponentName = g.ComponentName,
                Score = g.Score,
                MaxScore = g.MaxScore
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<StudentAttendanceRecordDto>> GetAttendanceRecordsAsync(Guid studentId)
    {
        return await _context.AttendanceRecords
            .Where(r => r.StudentId == studentId)
            .Include(r => r.Session)
            .ThenInclude(s => s!.Section)
            .ThenInclude(sec => sec!.Course)
            .Select(r => new StudentAttendanceRecordDto
            {
                CourseCode = r.Session!.Section!.Course!.Code,
                CourseName = r.Session.Section.Course.Name,
                SessionDate = r.Session.SessionDate,
                CheckInTime = r.CheckInTime,
                Status = r.Status
            })
            .ToListAsync();
    }
}
