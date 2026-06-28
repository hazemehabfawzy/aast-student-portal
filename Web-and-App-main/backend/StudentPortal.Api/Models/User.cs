using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Api.Models;

public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty; // admin, instructor, student

    // Navigation properties
    public Student? Student { get; set; }
    public Instructor? Instructor { get; set; }
}
