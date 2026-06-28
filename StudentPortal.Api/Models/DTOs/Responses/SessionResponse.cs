namespace StudentPortal.Api.Models.DTOs.Responses;

public class SessionResponse
{
    public Guid SessionId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string CurrentCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
