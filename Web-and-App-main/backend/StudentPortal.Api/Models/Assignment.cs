using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Api.Models;

public class Assignment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Body { get; set; }

    public DateTime? DueDate { get; set; }

    // Optional association to a section
    public int? SectionId { get; set; }
    public Section? Section { get; set; }

    // Creator (instructor)
    public Guid? InstructorId { get; set; }
    public Instructor? Instructor { get; set; }

    public List<AssignmentAttachment> Attachments { get; set; } = new();
}
