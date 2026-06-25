using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortal.Api.Models;

public class AttendanceSession
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SectionId { get; set; }

    [Required]
    public DateTime SessionDate { get; set; }

    [Required]
    public string SecretToken { get; set; } = string.Empty; // QR/PIN code value

    [Required]
    public bool IsActive { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
