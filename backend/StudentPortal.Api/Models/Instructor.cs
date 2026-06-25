using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Api.Models;

public class Instructor
{
    [Key]
    [ForeignKey(nameof(User))]
    public Guid Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty; // e.g. Dr., Eng.

    // Navigation properties
    public User? User { get; set; }
    public ICollection<Section> Sections { get; set; } = new List<Section>();
}
