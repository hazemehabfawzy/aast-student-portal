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
    Task<List<object>> FaceCheckInAsync(Guid sessionId, string imageBase64);
    Task<object> FaceStudentCheckInAsync(Guid studentId, string? sessionId, string imageBase64);
}
