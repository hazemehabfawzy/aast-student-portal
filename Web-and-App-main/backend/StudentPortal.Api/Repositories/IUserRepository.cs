using StudentPortal.Api.Models;

namespace StudentPortal.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<int> GetStudentCountAsync();
    Task<int> GetInstructorCountAsync();
    Task AddBulkAsync(IEnumerable<User> users, IEnumerable<Student> students, IEnumerable<Instructor> instructors);
}
