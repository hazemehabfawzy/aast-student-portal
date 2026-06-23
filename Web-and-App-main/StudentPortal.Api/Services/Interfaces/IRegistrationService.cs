namespace StudentPortal.Api.Services.Interfaces;

public interface IRegistrationService
{
    Task<object> GetAvailableSectionsAsync(string studentKeycloakId, Guid semesterId);
    Task<object> EnrollAsync(string studentKeycloakId, Guid sectionId);
    Task DropEnrollmentAsync(string studentKeycloakId, Guid enrollmentId);
    Task<object> GetStudentScheduleAsync(string studentKeycloakId);
}
