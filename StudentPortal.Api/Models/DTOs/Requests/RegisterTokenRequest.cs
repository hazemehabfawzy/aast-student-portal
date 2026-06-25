using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Api.Models.DTOs.Requests;

public class RegisterTokenRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string Platform { get; set; } = string.Empty;
}
