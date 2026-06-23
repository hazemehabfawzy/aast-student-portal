namespace StudentPortal.Api.Models.Entities;

public class GradingPolicy
{
    public Guid Id { get; set; }
    public Guid? CourseId { get; set; }
    public float Week7Weight { get; set; }
    public float Week12Weight { get; set; }
    public float PrefinalWeight { get; set; }
    public float FinalWeight { get; set; }

    // Navigation property
    public Course? Course { get; set; }
}
