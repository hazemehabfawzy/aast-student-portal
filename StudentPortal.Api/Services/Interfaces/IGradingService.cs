using StudentPortal.Api.Models.Entities;

namespace StudentPortal.Api.Services.Interfaces;

public interface IGradingService
{
    Task RecalculateResultAsync(Guid resultId);
    (bool IsValid, string? Reason) ValidatePolicyWeights(float w7, float w12, float wpf, float wf);
    Task<double> CalculateCumulativeGpaAsync(Guid studentId);
    string GetAcademicStanding(double gpa);
}
