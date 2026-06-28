namespace StudentPortal.Api.Models.Entities;

public class AttendanceSession
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public Guid InstructorId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Method { get; set; } = string.Empty; // "qr" or "pin"
    public string CurrentCode { get; set; } = string.Empty;
    public DateTime CodeExpiresAt { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public int RadiusMeters { get; set; }
    public int Week { get; set; } = 1;

    // Navigation properties
    public Section? Section { get; set; }
    public Instructor? Instructor { get; set; }
}
