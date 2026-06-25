using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Api.Models;

public class Course
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty; // e.g. CC411

    [Required]
    public string Name { get; set; } = string.Empty;

    public int Credits { get; set; }

    // Navigation properties
    public ICollection<Section> Sections { get; set; } = new List<Section>();
}
