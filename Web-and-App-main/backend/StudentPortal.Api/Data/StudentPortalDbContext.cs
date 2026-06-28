using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Models;

namespace StudentPortal.Api.Data;

public class StudentPortalDbContext : DbContext
{
    public StudentPortalDbContext(DbContextOptions<StudentPortalDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentAttachment> AssignmentAttachments => Set<AssignmentAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Composite Key for Enrollment
        modelBuilder.Entity<Enrollment>()
            .HasKey(e => new { e.StudentId, e.SectionId });

        // Configure 1-to-1 relations
        modelBuilder.Entity<Student>()
            .HasOne(s => s.User)
            .WithOne(u => u.Student)
            .HasForeignKey<Student>(s => s.Id)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Instructor>()
            .HasOne(i => i.User)
            .WithOne(u => u.Instructor)
            .HasForeignKey<Instructor>(i => i.Id)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Assignment>()
            .HasMany(a => a.Attachments)
            .WithOne(a => a.Assignment)
            .HasForeignKey(a => a.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
