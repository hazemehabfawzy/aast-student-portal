namespace StudentPortal.Api.Models.Entities;

public class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime CheckedInAt { get; set; }
    public string Status { get; set; } = string.Empty;

    // Navigation properties
    public AttendanceSession? Session { get; set; }
    public Student? Student { get; set; }
}
