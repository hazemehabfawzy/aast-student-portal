using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Api.Models.DTOs.Requests;

public class CheckInRequest
{
    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public double Lat { get; set; }

    [Required]
    public double Lng { get; set; }
}
