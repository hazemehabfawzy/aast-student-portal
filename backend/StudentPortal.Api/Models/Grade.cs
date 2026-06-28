using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Api.Models;

public class Grade
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid StudentId { get; set; }

    [Required]
    public int SectionId { get; set; }

    [Required]
    public string ComponentName { get; set; } = string.Empty; // e.g. Midterm, 7th Week, Final

    public double Score { get; set; }

    public double MaxScore { get; set; }

    // Navigation properties
    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }
}
