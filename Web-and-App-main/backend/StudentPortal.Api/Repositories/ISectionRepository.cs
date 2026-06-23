using StudentPortal.Api.Models;

namespace StudentPortal.Api.Repositories;

public interface ISectionRepository
{
    Task<IEnumerable<Section>> GetAllAsync();
    Task<Section?> GetByIdAsync(int id);
    Task AddAsync(Section section);
    Task UpdateAsync(Section section);
    Task DeleteAsync(int id);
    Task<int> GetCountAsync();
    Task<double> GetOverallAttendanceRateAsync();
}
