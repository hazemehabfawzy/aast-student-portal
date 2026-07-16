namespace StudentPortal.Api.Models.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public string SenderKeycloakId { get; set; } = "";
    public string SenderName { get; set; } = "";
    public string SenderRole { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
