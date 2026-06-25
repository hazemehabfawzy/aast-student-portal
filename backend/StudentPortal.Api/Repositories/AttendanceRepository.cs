using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models;

namespace StudentPortal.Api.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly StudentPortalDbContext _context;

    public AttendanceRepository(StudentPortalDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceSession?> GetSessionByIdAsync(int id)
    {
        return await _context.AttendanceSessions
            .Include(s => s.AttendanceRecords)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<AttendanceSession>> GetSessionsBySectionIdAsync(int sectionId)
    {
        return await _context.AttendanceSessions
            .Where(s => s.SectionId == sectionId)
            .OrderByDescending(s => s.SessionDate)
            .ToListAsync();
    }

    public async Task AddSessionAsync(AttendanceSession session)
    {
        await _context.AttendanceSessions.AddAsync(session);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSessionAsync(AttendanceSession session)
    {
        _context.AttendanceSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public async Task<AttendanceRecord?> GetRecordAsync(int sessionId, Guid studentId)
    {
        return await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.SessionId == sessionId && r.StudentId == studentId);
    }

    public async Task AddRecordAsync(AttendanceRecord record)
    {
        await _context.AttendanceRecords.AddAsync(record);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRecordAsync(AttendanceRecord record)
    {
        _context.AttendanceRecords.Update(record);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AttendanceRecord>> GetRecordsBySessionIdAsync(int sessionId)
    {
        return await _context.AttendanceRecords
            .Include(r => r.Student)
            .ThenInclude(s => s!.User)
            .Where(r => r.SessionId == sessionId)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
