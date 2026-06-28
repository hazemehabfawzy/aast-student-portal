using StudentPortal.Api.Models;
using StudentPortal.Api.Repositories;

namespace StudentPortal.Api.Services;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ISectionRepository _sectionRepository;

    public AdminService(
        IUserRepository userRepository,
        ICourseRepository courseRepository,
        ISectionRepository sectionRepository)
    {
        _userRepository = userRepository;
        _courseRepository = courseRepository;
        _sectionRepository = sectionRepository;
    }

    public async Task<AdminStatsDto> GetStatsAsync()
    {
        return new AdminStatsDto
        {
            StudentCount = await _userRepository.GetStudentCountAsync(),
            InstructorCount = await _userRepository.GetInstructorCountAsync(),
            CourseCount = await _courseRepository.GetCountAsync(),
            SectionCount = await _sectionRepository.GetCountAsync(),
            OverallAttendanceRate = await _sectionRepository.GetOverallAttendanceRateAsync()
        };
    }

    public async Task<int> BulkImportUsersAsync(IEnumerable<BulkImportItemDto> items)
    {
        var usersToImport = new List<User>();
        var studentsToImport = new List<Student>();
        var instructorsToImport = new List<Instructor>();

        foreach (var item in items)
        {
            // Skip if user already exists
            var existingUser = await _userRepository.GetByIdAsync(item.Id);
            if (existingUser != null)
            {
                continue;
            }

            var user = new User
            {
                Id = item.Id,
                Email = item.Email,
                FullName = item.FullName,
                Role = item.Role.ToLower()
            };

            usersToImport.Add(user);

            if (user.Role == "student")
            {
                studentsToImport.Add(new Student
                {
                    Id = user.Id,
                    RegistrationNumber = item.RegistrationNumber ?? string.Empty,
                    Gpa = 0.0 // Default baseline for imported students
                });
            }
            else if (user.Role == "instructor")
            {
                instructorsToImport.Add(new Instructor
                {
                    Id = user.Id,
                    Title = item.Title ?? "Eng."
                });
            }
        }

        if (usersToImport.Any())
        {
            await _userRepository.AddBulkAsync(usersToImport, studentsToImport, instructorsToImport);
        }

        return usersToImport.Count;
    }

    public async Task<IEnumerable<Course>> GetCoursesAsync() => await _courseRepository.GetAllAsync();

    public async Task<Course?> GetCourseByIdAsync(int id) => await _courseRepository.GetByIdAsync(id);

    public async Task<Course> CreateCourseAsync(CourseCreateDto dto)
    {
        var course = new Course
        {
            Code = dto.Code,
            Name = dto.Name,
            Credits = dto.Credits
        };
        await _courseRepository.AddAsync(course);
        return course;
    }

    public async Task<Course?> UpdateCourseAsync(int id, CourseUpdateDto dto)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        if (course == null) return null;

        course.Code = dto.Code;
        course.Name = dto.Name;
        course.Credits = dto.Credits;

        await _courseRepository.UpdateAsync(course);
        return course;
    }

    public async Task<bool> DeleteCourseAsync(int id)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        if (course == null) return false;

        await _courseRepository.DeleteAsync(id);
        return true;
    }

    public async Task<IEnumerable<Section>> GetSectionsAsync() => await _sectionRepository.GetAllAsync();

    public async Task<Section?> GetSectionByIdAsync(int id) => await _sectionRepository.GetByIdAsync(id);

    public async Task<Section> CreateSectionAsync(SectionCreateDto dto)
    {
        var section = new Section
        {
            CourseId = dto.CourseId,
            InstructorId = dto.InstructorId,
            Name = dto.Name,
            Term = dto.Term,
            Room = dto.Room,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime
        };
        await _sectionRepository.AddAsync(section);
        return section;
    }

    public async Task<Section?> UpdateSectionAsync(int id, SectionUpdateDto dto)
    {
        var section = await _sectionRepository.GetByIdAsync(id);
        if (section == null) return null;

        section.CourseId = dto.CourseId;
        section.InstructorId = dto.InstructorId;
        section.Name = dto.Name;
        section.Term = dto.Term;
        section.Room = dto.Room;
        section.DayOfWeek = dto.DayOfWeek;
        section.StartTime = dto.StartTime;
        section.EndTime = dto.EndTime;

        await _sectionRepository.UpdateAsync(section);
        return section;
    }

    public async Task<bool> DeleteSectionAsync(int id)
    {
        var section = await _sectionRepository.GetByIdAsync(id);
        if (section == null) return false;

        await _sectionRepository.DeleteAsync(id);
        return true;
    }
}
