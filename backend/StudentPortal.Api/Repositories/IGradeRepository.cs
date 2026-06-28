using StudentPortal.Api.Models;

namespace StudentPortal.Api.Repositories;

public interface IGradeRepository
{
    Task<Grade?> GetGradeAsync(Guid studentId, int sectionId, string componentName);
    Task AddGradeAsync(Grade grade);
    Task UpdateGradeAsync(Grade grade);
    Task<IEnumerable<Grade>> GetGradesBySectionIdAsync(int sectionId);
}
