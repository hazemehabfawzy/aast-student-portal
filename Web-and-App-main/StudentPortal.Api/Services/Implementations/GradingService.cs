using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.Entities;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Services.Implementations;

public class GradingService : IGradingService
{
    private readonly AppDbContext _context;

    public GradingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task RecalculateResultAsync(Guid resultId)
    {
        var result = await _context.Results
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Section)
                    .ThenInclude(s => s!.Course)
            .FirstOrDefaultAsync(r => r.Id == resultId);

        if (result == null || result.IsManualOverride) return;

        // Only calculate if all four scores are entered
        if (result.Week7Score == null || result.Week12Score == null ||
            result.PrefinalScore == null || result.FinalScore == null)
            return;

        // CORRECT formula: simple sum of raw scores
        // Week7 is out of 30, Week12 out of 20, Prefinal out of 10, Final out of 40
        // Total max = 100
        var total = result.Week7Score.Value
                  + result.Week12Score.Value
                  + result.PrefinalScore.Value
                  + result.FinalScore.Value;

        result.TotalScore = (float)Math.Round((double)total, 2);

        // Automatic F if final score < 12 (out of 40)
        if (result.FinalScore.Value < 12)
        {
            result.LetterGrade = "F";
        }
        else
        {
            // Look up letter grade from grade scale
            var gradeScale = await _context.GradeScales
                .Where(g => g.CountsTowardGpa &&
                            g.MinPercent <= result.TotalScore &&
                            g.MaxPercent >= result.TotalScore)
                .FirstOrDefaultAsync();

            result.LetterGrade = gradeScale?.Letter ?? "F";
        }

        _context.Results.Update(result);
        await _context.SaveChangesAsync();
    }

    public (bool IsValid, string? Reason) ValidatePolicyWeights(float w7, float w12, float wpf, float wf)
    {
        var sum = w7 + w12 + wpf + wf;
        if (Math.Abs(sum - 1.0f) > 0.001f)
        {
            return (false, $"Weights must sum to 1.0. Current sum: {sum}");
        }
        return (true, null);
    }

    public async Task<double> CalculateCumulativeGpaAsync(Guid studentId)
    {
        var results = await _context.Results
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Section)
                    .ThenInclude(s => s!.Course)
            .Where(r => r.Enrollment!.StudentId == studentId && r.Published)
            .ToListAsync();

        double totalPoints = 0.0;
        int totalCredits = 0;

        foreach (var result in results)
        {
            if (string.IsNullOrEmpty(result.LetterGrade))
            {
                continue;
            }

            var scale = await _context.GradeScales
                .FirstOrDefaultAsync(gs => gs.Letter == result.LetterGrade);

            if (scale != null && scale.CountsTowardGpa && scale.GpaPoints.HasValue)
            {
                var creditHours = result.Enrollment?.Section?.Course?.CreditHours ?? 0;
                totalPoints += scale.GpaPoints.Value * creditHours;
                totalCredits += creditHours;
            }
        }

        if (totalCredits == 0)
        {
            return 0.0;
        }

        return totalPoints / totalCredits;
    }

    public string GetAcademicStanding(double gpa)
    {
        if (gpa >= 3.70) return "Excellent";
        if (gpa >= 3.30) return "Very Good";
        if (gpa >= 2.70) return "Good";
        if (gpa >= 2.00) return "Pass";
        return "Fail";
    }
}
