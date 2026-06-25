using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Api.Models;

public class Enrollment
{
    public Guid StudentId { get; set; }
    public int SectionId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }
}
