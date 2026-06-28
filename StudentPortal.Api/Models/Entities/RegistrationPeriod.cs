namespace StudentPortal.Api.Models.Entities;

public class RegistrationPeriod
{
    public Guid Id { get; set; }
    public Guid SemesterId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsOpen { get; set; }

    // Navigation property
    public Semester? Semester { get; set; }
}
