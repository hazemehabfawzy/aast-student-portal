namespace StudentPortal.Api.Services.Implementations;

public static class PrerequisiteHelper
{
    public static bool IsPrerequisiteMet(string? prerequisiteCode, IReadOnlyCollection<string> passedCourseCodes)
    {
        if (string.IsNullOrWhiteSpace(prerequisiteCode))
        {
            return true;
        }

        var code = prerequisiteCode.Trim();

        if (code.Contains('|'))
        {
            return code.Split('|')
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .Any(c => passedCourseCodes.Contains(c));
        }

        if (code.Contains('&'))
        {
            return code.Split('&')
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .All(c => passedCourseCodes.Contains(c));
        }

        return passedCourseCodes.Contains(code);
    }
}
