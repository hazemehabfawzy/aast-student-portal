using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Api.Models;

public class AttendanceRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SessionId { get; set; }

    [Required]
    public Guid StudentId { get; set; }

    public DateTime? CheckInTime { get; set; }

    [Required]
    public string Status { get; set; } = "Absent"; // Present, Absent, Late

    // Navigation properties
    [ForeignKey(nameof(SessionId))]
    public AttendanceSession? Session { get; set; }

    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }
}
