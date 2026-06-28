using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Api.Models;

public class BulkImportItemDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(student|instructor)$", ErrorMessage = "Role must be 'student' or 'instructor'")]
    public string Role { get; set; } = string.Empty;

    // Student specific
    public string? RegistrationNumber { get; set; }

    // Instructor specific
    public string? Title { get; set; }
}

public class AdminStatsDto
{
    public int StudentCount { get; set; }
    public int InstructorCount { get; set; }
    public int CourseCount { get; set; }
    public int SectionCount { get; set; }
    public double OverallAttendanceRate { get; set; }
}

public class CourseCreateDto
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(1, 6)]
    public int Credits { get; set; }
}

public class CourseUpdateDto
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(1, 6)]
    public int Credits { get; set; }
}

public class SectionCreateDto
{
    [Required]
    public int CourseId { get; set; }

    [Required]
    public Guid InstructorId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Term { get; set; } = string.Empty;

    [Required]
    public string Room { get; set; } = string.Empty;

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }
}

public class SectionUpdateDto
{
    [Required]
    public int CourseId { get; set; }

    [Required]
    public Guid InstructorId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Term { get; set; } = string.Empty;

    [Required]
    public string Room { get; set; } = string.Empty;

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }
}

public class AttendanceSessionCreateDto
{
    [Range(1, 120)]
    public int ExpirationMinutes { get; set; } = 15;
}

public class AttendanceSessionResponseDto
{
    public int Id { get; set; }
    public int SectionId { get; set; }
    public DateTime SessionDate { get; set; }
    public string SecretToken { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class GradeSubmitDto
{
    [Required]
    public Guid StudentId { get; set; }

    [Required]
    public int SectionId { get; set; }

    [Required]
    public string ComponentName { get; set; } = string.Empty;

    [Range(0, 1000)]
    public double Score { get; set; }

    [Range(1, 1000)]
    public double MaxScore { get; set; }
}

public class StudentProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public double Gpa { get; set; }
}

public class StudentScheduleItemDto
{
    public int SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class StudentGradeDto
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public double Score { get; set; }
    public double MaxScore { get; set; }
}

public class StudentAttendanceRecordDto
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public string Status { get; set; } = string.Empty;
}


