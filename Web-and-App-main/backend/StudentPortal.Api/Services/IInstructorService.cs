using StudentPortal.Api.Models;

namespace StudentPortal.Api.Services;

public interface IInstructorService
{
    Task<IEnumerable<Section>> GetSectionsAsync(Guid instructorId);
    Task<bool> IsSectionOwnerAsync(int sectionId, Guid instructorId);
    Task<AttendanceSession> CreateAttendanceSessionAsync(int sectionId, AttendanceSessionCreateDto dto);
    Task<IEnumerable<AttendanceSession>> GetAttendanceSessionsAsync(int sectionId);
    Task<bool> ToggleSessionStatusAsync(int sessionId, bool isActive);
    Task<Grade> SubmitGradeAsync(GradeSubmitDto dto);
}
