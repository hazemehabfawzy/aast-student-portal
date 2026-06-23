using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Api.Models;

public class Student
{
    [Key]
    [ForeignKey(nameof(User))]
    public Guid Id { get; set; }

    [Required]
    public string RegistrationNumber { get; set; } = string.Empty;

    public double Gpa { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
