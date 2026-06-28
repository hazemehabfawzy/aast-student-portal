using StudentPortal.Api.Models;

namespace StudentPortal.Api.Data;

public static class DbSeeder
{
    public static void Seed(StudentPortalDbContext context)
    {
        // Check if database is already seeded
        if (context.Users.Any())
        {
            return;
        }

        var adminId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
        var instructorId = Guid.Parse("b2222222-2222-2222-2222-222222222222");
        var studentId = Guid.Parse("c3333333-3333-3333-3333-333333333333");

        // 1. Seed Users
        var adminUser = new User
        {
            Id = adminId,
            Email = "admin@aast.edu",
            FullName = "Admin User",
            Role = "admin"
        };

        var instructorUser = new User
        {
            Id = instructorId,
            Email = "instructor@aast.edu",
            FullName = "Dr. Instructor",
            Phone = "+201234567890",
            Role = "instructor"
        };

        var studentUser = new User
        {
            Id = studentId,
            Email = "student@aast.edu",
            FullName = "Student User",
            Phone = "+201011122233",
            Role = "student"
        };

        context.Users.AddRange(adminUser, instructorUser, studentUser);

        // 2. Seed Instructor details
        var instructor = new Instructor
        {
            Id = instructorId,
            Title = "Dr."
        };
        context.Instructors.Add(instructor);

        // 3. Seed Student details
        var student = new Student
        {
            Id = studentId,
            RegistrationNumber = "20101234",
            Gpa = 3.4
        };
        context.Students.Add(student);

        // 4. Seed Courses
        var courseNetworks = new Course { Code = "CC411", Name = "Computer Networks", Credits = 3 };
        var courseEmbedded = new Course { Code = "CC412", Name = "Embedded Systems", Credits = 3 };
        var courseSoftware = new Course { Code = "CC413", Name = "Software Engineering", Credits = 3 };

        context.Courses.AddRange(courseNetworks, courseEmbedded, courseSoftware);
        context.SaveChanges(); // Save to generate Course IDs

        // 5. Seed Sections
        var sectionNetworks = new Section
        {
            CourseId = courseNetworks.Id,
            InstructorId = instructorId,
            Name = "Sec 1",
            Term = "Fall 2026",
            Room = "R101",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(11, 0, 0)
        };

        var sectionEmbedded = new Section
        {
            CourseId = courseEmbedded.Id,
            InstructorId = instructorId,
            Name = "Sec 2",
            Term = "Fall 2026",
            Room = "R102",
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeSpan(11, 0, 0),
            EndTime = new TimeSpan(13, 0, 0)
        };

        context.Sections.AddRange(sectionNetworks, sectionEmbedded);
        context.SaveChanges(); // Save to generate Section IDs

        // 6. Seed Enrollments
        var enrollment1 = new Enrollment { StudentId = studentId, SectionId = sectionNetworks.Id };
        var enrollment2 = new Enrollment { StudentId = studentId, SectionId = sectionEmbedded.Id };

        context.Enrollments.AddRange(enrollment1, enrollment2);
        context.SaveChanges();
    }
}
