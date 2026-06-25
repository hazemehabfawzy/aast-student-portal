using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Models.Entities;

namespace StudentPortal.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<GradingPolicy> GradingPolicies => Set<GradingPolicy>();
    public DbSet<GradeScale> GradeScales => Set<GradeScale>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<RegistrationPeriod> RegistrationPeriods => Set<RegistrationPeriod>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique index on AttendanceRecord (SessionId, StudentId)
        modelBuilder.Entity<AttendanceRecord>()
            .HasIndex(r => new { r.SessionId, r.StudentId })
            .IsUnique();

        // Unique index on Student (StudentNumber)
        modelBuilder.Entity<Student>()
            .HasIndex(s => s.StudentNumber)
            .IsUnique();

        // Unique index on Student (KeycloakId)
        modelBuilder.Entity<Student>()
            .HasIndex(s => s.KeycloakId)
            .IsUnique();

        // Configure all FK relationships with DeleteBehavior.Restrict
        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
