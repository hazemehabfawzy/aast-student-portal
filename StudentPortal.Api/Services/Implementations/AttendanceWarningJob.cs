using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Services.Implementations;

public class AttendanceWarningJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AttendanceWarningJob> _logger;

    public AttendanceWarningJob(IServiceProvider serviceProvider, ILogger<AttendanceWarningJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("AttendanceWarningJob running checking loop...");
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var enrollments = await context.Enrollments
                        .Include(e => e.Section)
                            .ThenInclude(s => s!.Course)
                        .Where(e => !e.WarningSent)
                        .ToListAsync(stoppingToken);

                    foreach (var enrollment in enrollments)
                    {
                        var sectionId = enrollment.SectionId;
                        var studentId = enrollment.StudentId;

                        var totalSessions = await context.AttendanceSessions.CountAsync(s => s.SectionId == sectionId, stoppingToken);
                        var presentCount = await context.AttendanceRecords.CountAsync(
                            r => r.StudentId == studentId && r.Session!.SectionId == sectionId && r.Status == "present", 
                            stoppingToken);

                        if (totalSessions > 0)
                        {
                            double ratio = (double)presentCount / totalSessions;
                            if (ratio < 0.75)
                            {
                                var courseName = enrollment.Section?.Course?.Name ?? "Course";
                                await notificationService.SendPushAsync(
                                    studentId,
                                    "attendance_warning",
                                    "Attendance Warning",
                                    $"Your attendance in {courseName} is below 75%."
                                );

                                enrollment.WarningSent = true;
                                context.Enrollments.Update(enrollment);
                                await context.SaveChangesAsync(stoppingToken);

                                _logger.LogInformation("Sent attendance warning for student {StudentId} in course {CourseName}", studentId, courseName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during AttendanceWarningJob execution.");
            }

            // Sleep for 24 hours
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Deliberately empty, we are stopping
            }
        }
    }
}
