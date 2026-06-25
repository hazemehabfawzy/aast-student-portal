namespace StudentPortal.Api.Models.Entities;

public class Enrollment
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SectionId { get; set; }
    public bool WarningSent { get; set; }

    // Navigation properties
    public Student? Student { get; set; }
    public Section? Section { get; set; }
    public Result? Result { get; set; }
}
