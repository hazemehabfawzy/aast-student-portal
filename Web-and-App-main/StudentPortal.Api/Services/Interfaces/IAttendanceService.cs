using StudentPortal.Api.Models.DTOs.Requests;
using StudentPortal.Api.Models.DTOs.Responses;

namespace StudentPortal.Api.Services.Interfaces;

public interface IAttendanceService
{
    Task<SessionResponse> CreateSessionAsync(string instructorKeycloakId, CreateSessionRequest request);
    Task<string> RotateSessionCodeAsync(string instructorKeycloakId, Guid sessionId);
    Task CheckInAsync(string studentKeycloakId, CheckInRequest request);
    Task CloseSessionAsync(string instructorKeycloakId, Guid sessionId);
    Task<object> GetSectionAttendanceAsync(string instructorKeycloakId, Guid sectionId);
    Task<object> GetStudentMeAttendanceAsync(string studentKeycloakId);
    Task<List<FaceCheckInResult>> FaceCheckInAsync(string instructorKeycloakId, Guid sessionId, string base64Image);
    Task<object> FaceStudentCheckInAsync(string studentKeycloakId, Guid sessionId, string base64Image);
    Task DecidedWithdrawalAsync(string instructorKeycloakId, Guid sectionId, Guid enrollmentId, bool approve);
}

public class FaceCheckInResult
{
    public string Status { get; set; } = string.Empty; // "success", "not_registered", "no_face"
    public string? StudentKey { get; set; }
    public string? Name { get; set; }
    public double? Confidence { get; set; }
}
