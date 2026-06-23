using StudentPortal.Api.Models;

namespace StudentPortal.Api.Repositories;

public interface IAttendanceRepository
{
    Task<AttendanceSession?> GetSessionByIdAsync(int id);
    Task<IEnumerable<AttendanceSession>> GetSessionsBySectionIdAsync(int sectionId);
    Task AddSessionAsync(AttendanceSession session);
    Task UpdateSessionAsync(AttendanceSession session);
    Task<AttendanceRecord?> GetRecordAsync(int sessionId, Guid studentId);
    Task AddRecordAsync(AttendanceRecord record);
    Task UpdateRecordAsync(AttendanceRecord record);
    Task<IEnumerable<AttendanceRecord>> GetRecordsBySessionIdAsync(int sessionId);
    Task SaveChangesAsync();
}
