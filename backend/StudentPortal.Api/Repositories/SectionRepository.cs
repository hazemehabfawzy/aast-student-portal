using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models;

namespace StudentPortal.Api.Repositories;

public class SectionRepository : ISectionRepository
{
    private readonly StudentPortalDbContext _context;

    public SectionRepository(StudentPortalDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Section>> GetAllAsync()
    {
        return await _context.Sections
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .ThenInclude(i => i!.User)
            .ToListAsync();
    }

    public async Task<Section?> GetByIdAsync(int id)
    {
        return await _context.Sections
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .ThenInclude(i => i!.User)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddAsync(Section section)
    {
        await _context.Sections.AddAsync(section);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Section section)
    {
        _context.Sections.Update(section);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var section = await _context.Sections.FindAsync(id);
        if (section != null)
        {
            _context.Sections.Remove(section);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Sections.CountAsync();
    }

    public async Task<double> GetOverallAttendanceRateAsync()
    {
        var total = await _context.AttendanceRecords.CountAsync();
        if (total == 0)
        {
            return 0.0;
        }

        var presentOrLate = await _context.AttendanceRecords
            .CountAsync(r => r.Status == "Present" || r.Status == "Late");

        return (double)presentOrLate / total * 100.0;
    }
}
