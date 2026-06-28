using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models;

namespace StudentPortal.Api.Repositories;

public class GradeRepository : IGradeRepository
{
    private readonly StudentPortalDbContext _context;

    public GradeRepository(StudentPortalDbContext context)
    {
        _context = context;
    }

    public async Task<Grade?> GetGradeAsync(Guid studentId, int sectionId, string componentName)
    {
        return await _context.Grades
            .FirstOrDefaultAsync(g => g.StudentId == studentId && g.SectionId == sectionId && g.ComponentName == componentName);
    }

    public async Task AddGradeAsync(Grade grade)
    {
        await _context.Grades.AddAsync(grade);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateGradeAsync(Grade grade)
    {
        _context.Grades.Update(grade);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Grade>> GetGradesBySectionIdAsync(int sectionId)
    {
        return await _context.Grades
            .Include(g => g.Student)
            .ThenInclude(s => s!.User)
            .Where(g => g.SectionId == sectionId)
            .ToListAsync();
    }
}
