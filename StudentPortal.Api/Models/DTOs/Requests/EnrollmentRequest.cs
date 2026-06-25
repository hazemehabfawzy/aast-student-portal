using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Api.Models.DTOs.Requests;

public class EnrollmentRequest
{
    [Required]
    public Guid SectionId { get; set; }
}
