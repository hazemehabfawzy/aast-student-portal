using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Api.Models.DTOs.Requests;

public class UpdateScoresRequest
{
    [Range(0, 30, ErrorMessage = "Week 7 score must be between 0 and 30.")]
    public float? Week7Score { get; set; }

    [Range(0, 20, ErrorMessage = "Week 12 score must be between 0 and 20.")]
    public float? Week12Score { get; set; }

    [Range(0, 10, ErrorMessage = "Prefinal score must be between 0 and 10.")]
    public float? PrefinalScore { get; set; }

    [Range(0, 40, ErrorMessage = "Final score must be between 0 and 40.")]
    public float? FinalScore { get; set; }

    [Required]
    public bool IsManualOverride { get; set; }

    public string? LetterGrade { get; set; }
}
