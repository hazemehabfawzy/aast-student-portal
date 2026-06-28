namespace StudentPortal.Api.Models.Entities;

public class Course
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public Guid DepartmentId { get; set; }
    public int SemesterNumber { get; set; }
    public string? PrerequisiteCode { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property
    public Department? Department { get; set; }
}
