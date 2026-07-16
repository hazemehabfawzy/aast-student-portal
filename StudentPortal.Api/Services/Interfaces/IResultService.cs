using StudentPortal.Api.Models.DTOs.Requests;
using StudentPortal.Api.Services.Implementations;

namespace StudentPortal.Api.Services.Interfaces;

public interface IResultService
{
    Task<object> GetMyResultsAsync(string studentKeycloakId, Guid? semesterId = null);
    Task<object> GetSectionResultsAsync(string instructorKeycloakId, Guid sectionId);
    Task UpdateResultAsync(string instructorKeycloakId, Guid enrollmentId, UpdateScoresRequest request);
    Task PublishResultAsync(string userKeycloakId, string role, Guid enrollmentId);
    Task PublishSectionResultsAsync(string instructorKeycloakId, Guid sectionId);
    Task<List<object>> GetAtRiskStudentsAsync(string instructorKeycloakId, Guid sectionId, GradePredictionClient prediction);
}
