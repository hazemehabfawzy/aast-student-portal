namespace StudentPortal.Api.Models.DTOs.Requests;

public class FaceCheckInDto
{
    public string  Image     { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}
