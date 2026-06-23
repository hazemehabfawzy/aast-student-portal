using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models;

namespace StudentPortal.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly StudentPortalDbContext _context;

    public UserRepository(StudentPortalDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<int> GetStudentCountAsync()
    {
        return await _context.Students.CountAsync();
    }

    public async Task<int> GetInstructorCountAsync()
    {
        return await _context.Instructors.CountAsync();
    }

    public async Task AddBulkAsync(IEnumerable<User> users, IEnumerable<Student> students, IEnumerable<Instructor> instructors)
    {
        await _context.Users.AddRangeAsync(users);
        await _context.Students.AddRangeAsync(students);
        await _context.Instructors.AddRangeAsync(instructors);
        await _context.SaveChangesAsync();
    }
}
