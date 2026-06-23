namespace StudentPortal.Api.Models.Entities;

public class DeviceToken
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string FcmToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }

    // Navigation property
    public Student? Student { get; set; }
}
