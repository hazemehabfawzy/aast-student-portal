using StudentPortal.Api.Models;

namespace StudentPortal.Api.Services;

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync();
    Task<int> BulkImportUsersAsync(IEnumerable<BulkImportItemDto> items);
    Task<IEnumerable<Course>> GetCoursesAsync();
    Task<Course?> GetCourseByIdAsync(int id);
    Task<Course> CreateCourseAsync(CourseCreateDto dto);
    Task<Course?> UpdateCourseAsync(int id, CourseUpdateDto dto);
    Task<bool> DeleteCourseAsync(int id);
    Task<IEnumerable<Section>> GetSectionsAsync();
    Task<Section?> GetSectionByIdAsync(int id);
    Task<Section> CreateSectionAsync(SectionCreateDto dto);
    Task<Section?> UpdateSectionAsync(int id, SectionUpdateDto dto);
    Task<bool> DeleteSectionAsync(int id);
}
