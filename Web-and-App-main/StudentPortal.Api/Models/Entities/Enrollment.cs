namespace StudentPortal.Api.Models.Entities;

public class Enrollment
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SectionId { get; set; }
    public bool WarningSent { get; set; }
    public bool FaceAttendanceEnabled { get; set; }
    public bool IsWithdrawn { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public bool WithdrawalPending { get; set; }

    // Navigation properties
    public Student? Student { get; set; }
    public Section? Section { get; set; }
    public Result? Result { get; set; }
}
