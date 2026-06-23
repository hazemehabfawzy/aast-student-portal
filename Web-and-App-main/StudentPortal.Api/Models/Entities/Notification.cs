namespace StudentPortal.Api.Models.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public Student? Student { get; set; }
}
