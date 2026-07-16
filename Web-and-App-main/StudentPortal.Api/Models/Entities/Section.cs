namespace StudentPortal.Api.Models.Entities;

public class Section
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid InstructorId { get; set; }
    public Guid SemesterId { get; set; }
    public string ScheduleJson { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public Course? Course { get; set; }
    public Instructor? Instructor { get; set; }
    public Semester? Semester { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
