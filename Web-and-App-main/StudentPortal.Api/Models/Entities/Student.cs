namespace StudentPortal.Api.Models.Entities;

public class Student
{
    public Guid Id { get; set; }
    public string KeycloakId { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public int YearLevel { get; set; }

    // Navigation property
    public Department? Department { get; set; }

    public string? FaceEncodingKey { get; set; }
}
