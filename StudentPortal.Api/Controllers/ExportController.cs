using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudentPortal.Api.Data;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Controllers;

[ApiController]
[Route("api")]
public class ExportController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IGradingService _gradingService;

    public ExportController(AppDbContext context, IGradingService gradingService)
    {
        _context = context;
        _gradingService = gradingService;
    }

    private string GetCurrentKeycloakId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
               ?? User.FindFirst("sub")?.Value 
               ?? throw new UnauthorizedAccessException("User identification claim is missing.");
    }

    [HttpGet("sections/{sectionId:guid}/attendance/export")]
    [Authorize(Roles = "admin,instructor")]
    public async Task<IActionResult> ExportAttendance(Guid sectionId, [FromQuery] string format)
    {
        var section = await _context.Sections
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .Include(s => s.Semester)
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        if (section == null)
        {
            return NotFound(new { message = "Section not found." });
        }

        // Validate instructor permission
        var keycloakId = GetCurrentKeycloakId();
        if (User.IsInRole("instructor"))
        {
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == keycloakId);
            if (instructor == null || section.InstructorId != instructor.Id)
            {
                return StatusCode(403, new { message = "You do not own this section." });
            }
        }

        var totalSessions = await _context.AttendanceSessions.CountAsync(s => s.SectionId == sectionId);
        var enrollments = await _context.Enrollments
            .Include(e => e.Student)
            .Where(e => e.SectionId == sectionId)
            .ToListAsync();

        var studentAttendanceData = enrollments.Select(e =>
        {
            var presentCount = _context.AttendanceRecords
                .Count(r => r.StudentId == e.StudentId && r.Session!.SectionId == sectionId && r.Status == "present");
            var percentage = totalSessions == 0 ? 100.0 : (double)presentCount / totalSessions * 100.0;
            return new
            {
                StudentNumber = e.Student?.StudentNumber ?? string.Empty,
                FullName = e.Student?.FullName ?? string.Empty,
                PresentCount = presentCount,
                Percentage = percentage
            };
        }).OrderBy(s => s.StudentNumber).ToList();

        if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Attendance Report");
            worksheet.Cell(1, 1).Value = "Student Number";
            worksheet.Cell(1, 2).Value = "Full Name";
            worksheet.Cell(1, 3).Value = "Sessions";
            worksheet.Cell(1, 4).Value = "Percentage";

            // Headers formatting
            worksheet.Row(1).Style.Font.Bold = true;

            int row = 2;
            foreach (var student in studentAttendanceData)
            {
                worksheet.Cell(row, 1).Value = student.StudentNumber;
                worksheet.Cell(row, 2).Value = student.FullName;
                worksheet.Cell(row, 3).Value = $"{student.PresentCount} / {totalSessions}";
                worksheet.Cell(row, 4).Value = $"{student.Percentage:F1}%";
                row++;
            }
            worksheet.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"attendance_section_{sectionId}.xlsx");
        }
        else if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Attendance Report").SemiBold().FontSize(18);
                        col.Item().Text($"Course: {section.Course?.Code} - {section.Course?.Name}").FontSize(11);
                        col.Item().Text($"Instructor: {section.Instructor?.FullName}").FontSize(11);
                        col.Item().Text($"Semester: {section.Semester?.Name}").FontSize(11);
                        col.Item().PaddingTop(10).LineHorizontal(1);
                    });

                    page.Content().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Student Number
                            columns.RelativeColumn(4); // Full Name
                            columns.RelativeColumn(2); // Sessions
                            columns.RelativeColumn(2); // Percentage
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Student Number").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Full Name").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Sessions").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Percentage").SemiBold();
                        });

                        foreach (var student in studentAttendanceData)
                        {
                            table.Cell().Padding(5).Text(student.StudentNumber);
                            table.Cell().Padding(5).Text(student.FullName);
                            table.Cell().Padding(5).Text($"{student.PresentCount} / {totalSessions}");
                            table.Cell().Padding(5).Text($"{student.Percentage:F1}%");
                        }
                    });
                });
            });

            var pdfBytes = doc.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"attendance_section_{sectionId}.pdf");
        }

        return BadRequest(new { message = "Unsupported export format." });
    }

    [HttpGet("sections/{sectionId:guid}/results/export")]
    [Authorize(Roles = "admin,instructor")]
    public async Task<IActionResult> ExportResults(Guid sectionId, [FromQuery] string format)
    {
        var section = await _context.Sections
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .Include(s => s.Semester)
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        if (section == null)
        {
            return NotFound(new { message = "Section not found." });
        }

        // Validate instructor permission
        var keycloakId = GetCurrentKeycloakId();
        if (User.IsInRole("instructor"))
        {
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == keycloakId);
            if (instructor == null || section.InstructorId != instructor.Id)
            {
                return StatusCode(403, new { message = "You do not own this section." });
            }
        }

        var results = await _context.Results
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Student)
            .Where(r => r.Enrollment!.SectionId == sectionId)
            .ToListAsync();

        var roster = results.Select(r => new
        {
            StudentNumber = r.Enrollment?.Student?.StudentNumber ?? string.Empty,
            FullName = r.Enrollment?.Student?.FullName ?? string.Empty,
            Week7 = r.Week7Score,
            Week12 = r.Week12Score,
            Prefinal = r.PrefinalScore,
            Final = r.FinalScore,
            Total = r.TotalScore,
            Grade = r.LetterGrade ?? string.Empty
        }).OrderBy(s => s.StudentNumber).ToList();

        if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Grade Roster");
            worksheet.Cell(1, 1).Value = "Student Number";
            worksheet.Cell(1, 2).Value = "Full Name";
            worksheet.Cell(1, 3).Value = "Week 7";
            worksheet.Cell(1, 4).Value = "Week 12";
            worksheet.Cell(1, 5).Value = "Prefinal";
            worksheet.Cell(1, 6).Value = "Final";
            worksheet.Cell(1, 7).Value = "Total";
            worksheet.Cell(1, 8).Value = "Letter Grade";

            worksheet.Row(1).Style.Font.Bold = true;

            int row = 2;
            foreach (var student in roster)
            {
                worksheet.Cell(row, 1).Value = student.StudentNumber;
                worksheet.Cell(row, 2).Value = student.FullName;
                worksheet.Cell(row, 3).Value = student.Week7;
                worksheet.Cell(row, 4).Value = student.Week12;
                worksheet.Cell(row, 5).Value = student.Prefinal;
                worksheet.Cell(row, 6).Value = student.Final;
                worksheet.Cell(row, 7).Value = student.Total;
                worksheet.Cell(row, 8).Value = student.Grade;
                row++;
            }
            worksheet.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"results_section_{sectionId}.xlsx");
        }
        else if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Grade Roster").SemiBold().FontSize(18);
                        col.Item().Text($"Course: {section.Course?.Code} - {section.Course?.Name}").FontSize(11);
                        col.Item().Text($"Instructor: {section.Instructor?.FullName}").FontSize(11);
                        col.Item().Text($"Semester: {section.Semester?.Name}").FontSize(11);
                        col.Item().PaddingTop(10).LineHorizontal(1);
                    });

                    page.Content().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Student Number
                            columns.RelativeColumn(3); // Full Name
                            columns.RelativeColumn(1); // W7
                            columns.RelativeColumn(1); // W12
                            columns.RelativeColumn(1); // Pre
                            columns.RelativeColumn(1); // Final
                            columns.RelativeColumn(1); // Total
                            columns.RelativeColumn(1); // Grade
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Student Number").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Full Name").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("W7").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("W12").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Pre").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Final").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Total").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Grade").SemiBold();
                        });

                        foreach (var r in roster)
                        {
                            table.Cell().Padding(5).Text(r.StudentNumber);
                            table.Cell().Padding(5).Text(r.FullName);
                            table.Cell().Padding(5).Text(r.Week7?.ToString("F1") ?? "-");
                            table.Cell().Padding(5).Text(r.Week12?.ToString("F1") ?? "-");
                            table.Cell().Padding(5).Text(r.Prefinal?.ToString("F1") ?? "-");
                            table.Cell().Padding(5).Text(r.Final?.ToString("F1") ?? "-");
                            table.Cell().Padding(5).Text(r.Total?.ToString("F1") ?? "-");
                            table.Cell().Padding(5).Text(r.Grade);
                        }
                    });
                });
            });

            var pdfBytes = doc.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"results_section_{sectionId}.pdf");
        }

        return BadRequest(new { message = "Unsupported export format." });
    }

    [HttpGet("students/{id:guid}/transcript/export")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportTranscript(Guid id, [FromQuery] string format)
    {
        if (!format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only PDF format is supported for transcript exports." });
        }

        var student = await _context.Students
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
        {
            return NotFound(new { message = "Student not found." });
        }

        var results = await _context.Results
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Section)
                    .ThenInclude(sec => sec!.Course)
            .Include(r => r.Enrollment)
                .ThenInclude(e => e!.Section)
                    .ThenInclude(sec => sec!.Semester)
            .Where(r => r.Enrollment!.StudentId == id)
            .ToListAsync();

        var semesters = results
            .GroupBy(r => r.Enrollment!.Section!.Semester)
            .OrderBy(g => g.Key?.StartDate)
            .ToList();

        var cumulativeGpa = await _gradingService.CalculateCumulativeGpaAsync(id);
        var standing = _gradingService.GetAcademicStanding(cumulativeGpa);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Official Academic Transcript").SemiBold().FontSize(20).FontColor(Colors.Blue.Darken3);
                    col.Item().PaddingVertical(5).LineHorizontal(1);
                    col.Item().Text($"Student Name: {student.FullName}").SemiBold();
                    col.Item().Text($"Student Number: {student.StudentNumber}");
                    col.Item().Text($"Department: {student.Department?.Name ?? "N/A"}");
                    col.Item().Text($"Cumulative GPA: {cumulativeGpa:F2}");
                    col.Item().Text($"Academic Standing: {standing}");
                    col.Item().PaddingTop(10).LineHorizontal(1);
                });

                page.Content().Column(col =>
                {
                    foreach (var semGroup in semesters)
                    {
                        var semester = semGroup.Key;
                        if (semester == null) continue;

                        col.Item().PaddingTop(15).Text(semester.Name).SemiBold().FontSize(13).FontColor(Colors.Blue.Darken2);
                        
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Course Code
                                columns.RelativeColumn(5); // Course Name
                                columns.RelativeColumn(1.5f); // Credits
                                columns.RelativeColumn(1.5f); // Grade
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Course Code").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Course Name").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Credits").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Grade").SemiBold();
                            });

                            foreach (var r in semGroup)
                            {
                                var course = r.Enrollment?.Section?.Course;
                                table.Cell().Padding(5).Text(course?.Code ?? "-");
                                table.Cell().Padding(5).Text(course?.Name ?? "-");
                                table.Cell().Padding(5).Text(course?.CreditHours.ToString() ?? "0");
                                table.Cell().Padding(5).Text(r.LetterGrade ?? "-");
                            }
                        });
                    }
                });
            });
        });

        var pdfBytes = doc.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"transcript_{student.StudentNumber}.pdf");
    }
}
