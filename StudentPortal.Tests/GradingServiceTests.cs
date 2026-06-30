using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.Entities;
using StudentPortal.Api.Services.Implementations;
using Xunit;

namespace StudentPortal.Tests;

public class GradingServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private async Task SeedGradeScalesAndDefaultPolicyAsync(AppDbContext context)
    {
        await DbInitializer.SeedAsync(context);
    }

    [Fact]
    public async Task RecalculateResult_CorrectTotalScoreAndLetter_30_20_10_40_Weights()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        await SeedGradeScalesAndDefaultPolicyAsync(context);

        var dept = new Department { Id = Guid.NewGuid(), Name = "Computer Engineering" };
        var course = new Course { Id = Guid.NewGuid(), Code = "CC111", Name = "Intro", CreditHours = 3, DepartmentId = dept.Id };
        var instructor = new Instructor { Id = Guid.NewGuid(), KeycloakId = "inst", FullName = "Dr. Ahmed", DepartmentId = dept.Id };
        var semester = new Semester { Id = Guid.NewGuid(), Name = "Fall", IsCurrent = true };
        var section = new Section { Id = Guid.NewGuid(), CourseId = course.Id, InstructorId = instructor.Id, SemesterId = semester.Id, IsActive = true };
        var student = new Student { Id = Guid.NewGuid(), StudentNumber = "123", FullName = "Omar", DepartmentId = dept.Id };
        var enrollment = new Enrollment { Id = Guid.NewGuid(), StudentId = student.Id, SectionId = section.Id };
        var result = new Result
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollment.Id,
            Week7Score = 27f,    // max 30
            Week12Score = 18f,   // max 20
            PrefinalScore = 9f,  // max 10
            FinalScore = 36f     // max 40
            // Sum: 27 + 18 + 9 + 36 = 90
        };

        await context.Departments.AddAsync(dept);
        await context.Courses.AddAsync(course);
        await context.Instructors.AddAsync(instructor);
        await context.Semesters.AddAsync(semester);
        await context.Sections.AddAsync(section);
        await context.Students.AddAsync(student);
        await context.Enrollments.AddAsync(enrollment);
        await context.Results.AddAsync(result);
        await context.SaveChangesAsync();

        var service = new GradingService(context);

        // Act
        await service.RecalculateResultAsync(result.Id);

        // Assert
        var updatedResult = await context.Results.FindAsync(result.Id);
        Assert.NotNull(updatedResult);
        Assert.Equal(90f, updatedResult.TotalScore);
        Assert.Equal("A-", updatedResult.LetterGrade); // 90 is between 89 and 92
    }

    [Theory]
    [InlineData(97f, "A+")]
    [InlineData(96f, "A")]
    [InlineData(60f, "D")]
    [InlineData(59f, "F")]
    public async Task RecalculateResult_BoundaryValuesLookup(float totalScore, string expectedLetter)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        await SeedGradeScalesAndDefaultPolicyAsync(context);

        var dept = new Department { Id = Guid.NewGuid(), Name = "Computer Engineering" };
        var course = new Course { Id = Guid.NewGuid(), Code = "CC111", Name = "Intro", CreditHours = 3, DepartmentId = dept.Id };
        var instructor = new Instructor { Id = Guid.NewGuid(), KeycloakId = "inst", FullName = "Dr. Ahmed", DepartmentId = dept.Id };
        var semester = new Semester { Id = Guid.NewGuid(), Name = "Fall", IsCurrent = true };
        var section = new Section { Id = Guid.NewGuid(), CourseId = course.Id, InstructorId = instructor.Id, SemesterId = semester.Id, IsActive = true };
        var student = new Student { Id = Guid.NewGuid(), StudentNumber = "123", FullName = "Omar", DepartmentId = dept.Id };
        var enrollment = new Enrollment { Id = Guid.NewGuid(), StudentId = student.Id, SectionId = section.Id };
        
        // Decompose totalScore into valid raw-point components:
        // Week7 max=30, Week12 max=20, Prefinal max=10, Final max=40
        // Use floor fractions so sum always equals totalScore exactly.
        float w7  = MathF.Floor(totalScore * 0.30f);
        float w12 = MathF.Floor(totalScore * 0.20f);
        float wpf = MathF.Floor(totalScore * 0.10f);
        float wf  = totalScore - w7 - w12 - wpf;  // absorbs rounding remainder
        var result = new Result
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollment.Id,
            Week7Score    = w7,
            Week12Score   = w12,
            PrefinalScore = wpf,
            FinalScore    = wf
        };

        await context.Departments.AddAsync(dept);
        await context.Courses.AddAsync(course);
        await context.Instructors.AddAsync(instructor);
        await context.Semesters.AddAsync(semester);
        await context.Sections.AddAsync(section);
        await context.Students.AddAsync(student);
        await context.Enrollments.AddAsync(enrollment);
        await context.Results.AddAsync(result);
        await context.SaveChangesAsync();

        var service = new GradingService(context);

        // Act
        await service.RecalculateResultAsync(result.Id);

        // Assert
        var updatedResult = await context.Results.FindAsync(result.Id);
        Assert.NotNull(updatedResult);
        Assert.Equal(expectedLetter, updatedResult.LetterGrade);
    }

    [Theory]
    [InlineData(0.3f, 0.2f, 0.1f, 0.4f, true)]
    [InlineData(0.3f, 0.2f, 0.1f, 0.3f, false)] // Sums to 0.9
    [InlineData(0.3f, 0.2f, 0.1f, 0.5f, false)] // Sums to 1.1
    public void ValidatePolicyWeights_ToleranceCheck(float w7, float w12, float wpf, float wf, bool expectedValid)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new GradingService(context);

        // Act
        var (isValid, _) = service.ValidatePolicyWeights(w7, w12, wpf, wf);

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public async Task CalculateCumulativeGpa_ExcludesNonGpaGrades_IncludesF()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        await SeedGradeScalesAndDefaultPolicyAsync(context);

        var dept = new Department { Id = Guid.NewGuid(), Name = "Computer Engineering" };
        var course1 = new Course { Id = Guid.NewGuid(), Code = "CC111", Name = "Intro", CreditHours = 3, DepartmentId = dept.Id };
        var course2 = new Course { Id = Guid.NewGuid(), Code = "CC112", Name = "Structured", CreditHours = 4, DepartmentId = dept.Id };
        var course3 = new Course { Id = Guid.NewGuid(), Code = "CC211", Name = "OOP", CreditHours = 3, DepartmentId = dept.Id }; // Excluded Grade: W (0 credits towards GPA)
        var course4 = new Course { Id = Guid.NewGuid(), Code = "CC212", Name = "DSA", CreditHours = 2, DepartmentId = dept.Id }; // Excluded Grade: P

        var instructor = new Instructor { Id = Guid.NewGuid(), KeycloakId = "inst", FullName = "Dr. Ahmed", DepartmentId = dept.Id };
        var semester = new Semester { Id = Guid.NewGuid(), Name = "Fall", IsCurrent = true };
        
        var section1 = new Section { Id = Guid.NewGuid(), CourseId = course1.Id, InstructorId = instructor.Id, SemesterId = semester.Id, IsActive = true };
        var section2 = new Section { Id = Guid.NewGuid(), CourseId = course2.Id, InstructorId = instructor.Id, SemesterId = semester.Id, IsActive = true };
        var section3 = new Section { Id = Guid.NewGuid(), CourseId = course3.Id, InstructorId = instructor.Id, SemesterId = semester.Id, IsActive = true };
        var section4 = new Section { Id = Guid.NewGuid(), CourseId = course4.Id, InstructorId = instructor.Id, SemesterId = semester.Id, IsActive = true };

        var student = new Student { Id = Guid.NewGuid(), StudentNumber = "123", FullName = "Omar", DepartmentId = dept.Id };
        
        var env1 = new Enrollment { Id = Guid.NewGuid(), StudentId = student.Id, SectionId = section1.Id };
        var env2 = new Enrollment { Id = Guid.NewGuid(), StudentId = student.Id, SectionId = section2.Id };
        var env3 = new Enrollment { Id = Guid.NewGuid(), StudentId = student.Id, SectionId = section3.Id };
        var env4 = new Enrollment { Id = Guid.NewGuid(), StudentId = student.Id, SectionId = section4.Id };

        // Result 1: A (3.8333 points, 3 credits) => 11.4999
        var res1 = new Result { Id = Guid.NewGuid(), EnrollmentId = env1.Id, LetterGrade = "A", Published = true };
        // Result 2: F (0.0000 points, 4 credits) => 0.0000
        var res2 = new Result { Id = Guid.NewGuid(), EnrollmentId = env2.Id, LetterGrade = "F", Published = true };
        // Result 3: W (Null points, 3 credits - countsTowardGpa is false) => Should be completely ignored
        var res3 = new Result { Id = Guid.NewGuid(), EnrollmentId = env3.Id, LetterGrade = "W", Published = true };
        // Result 4: P (Null points, 2 credits - countsTowardGpa is false) => Should be completely ignored
        var res4 = new Result { Id = Guid.NewGuid(), EnrollmentId = env4.Id, LetterGrade = "P", Published = true };

        await context.Departments.AddAsync(dept);
        await context.Courses.AddRangeAsync(course1, course2, course3, course4);
        await context.Instructors.AddAsync(instructor);
        await context.Semesters.AddAsync(semester);
        await context.Sections.AddRangeAsync(section1, section2, section3, section4);
        await context.Students.AddAsync(student);
        await context.Enrollments.AddRangeAsync(env1, env2, env3, env4);
        await context.Results.AddRangeAsync(res1, res2, res3, res4);
        await context.SaveChangesAsync();

        var service = new GradingService(context);

        // Act
        var gpa = await service.CalculateCumulativeGpaAsync(student.Id);

        // Assert
        // Expected GPA numerator = 3.8333 * 3 + 0.0 * 4 = 11.4999
        // Expected GPA denominator = 3 + 4 = 7
        // Expected GPA = 11.4999 / 7 = 1.64284
        Assert.Equal(11.4999 / 7.0, gpa, precision: 4);
    }
}
