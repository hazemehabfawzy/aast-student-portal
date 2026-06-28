using StudentPortal.Api.Models;

namespace StudentPortal.Api.Services;

public interface IStudentService
{
    Task<StudentProfileDto?> GetProfileAsync(Guid studentId);
    Task<IEnumerable<StudentScheduleItemDto>> GetScheduleAsync(Guid studentId);
    Task<IEnumerable<StudentGradeDto>> GetGradesAsync(Guid studentId);
    Task<IEnumerable<StudentAttendanceRecordDto>> GetAttendanceRecordsAsync(Guid studentId);
}
