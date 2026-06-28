namespace StudentPortal.Api.Models.Entities;

public class Instructor
{
    public Guid Id { get; set; }
    public string KeycloakId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }

    // Navigation property
    public Department? Department { get; set; }
}
