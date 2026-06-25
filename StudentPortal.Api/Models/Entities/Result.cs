namespace StudentPortal.Api.Models.Entities;

public class Result
{
    public Guid Id { get; set; }
    public Guid EnrollmentId { get; set; }
    public float? Week7Score { get; set; }
    public float? Week12Score { get; set; }
    public float? PrefinalScore { get; set; }
    public float? FinalScore { get; set; }
    public float? TotalScore { get; set; }
    public string? LetterGrade { get; set; }
    public bool Published { get; set; }
    public bool IsManualOverride { get; set; }

    // Navigation property
    public Enrollment? Enrollment { get; set; }
}
