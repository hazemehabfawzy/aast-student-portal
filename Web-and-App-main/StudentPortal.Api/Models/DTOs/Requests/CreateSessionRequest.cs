using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Api.Models.DTOs.Requests;

public class CreateSessionRequest
{
    [Required]
    public Guid SectionId { get; set; }

    public int? DurationMinutes { get; set; }

    [Required]
    [RegularExpression("^(qr|pin|face)$", ErrorMessage = "Method must be 'qr', 'pin', or 'face'.")]
    public string Method { get; set; } = string.Empty;

    [Required]
    public double Lat { get; set; }

    [Required]
    public double Lng { get; set; }

    [Required]
    [Range(1, 1000, ErrorMessage = "Radius must be between 1 and 1000 meters.")]
    public int RadiusMeters { get; set; }

    [Range(1, 15, ErrorMessage = "Week must be between 1 and 15.")]
    public int Week { get; set; } = 1;
}
