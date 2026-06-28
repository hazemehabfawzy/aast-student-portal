using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudentPortal.Api.Models.Entities;

namespace StudentPortal.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, IServiceProvider? sp = null)
    {
        // 1. Seed Grade Scales
        if (!await db.GradeScales.AnyAsync())
        {
            var scales = new List<GradeScale>
            {
                new() { Id = Guid.NewGuid(), Letter = "A+", MinPercent = 97f, MaxPercent = 100f, GpaPoints = 4.0000f, CountsTowardGpa = true },
                new() { Id = Guid.NewGuid(), Letter = "A", MinPercent = 93f, MaxPercent = 96f, GpaPoints = 3.8333f, CountsTowardGpa = true },
                new() { Id = Guid.NewGuid(), Letter = "A-", MinPercent = 89f, MaxPercent = 92f, GpaPoints = 3.6667f, CountsTowardGpa = true },
                new() { Id = Guid.NewGuid(), Letter = "B+", MinPercent = 84f, MaxPercent = 88f, GpaPoints = 3.3333f, CountsTowardGpa = true },
                new() { Id = Guid.NewGuid(), Letter = "B", MinPercent = 80f, MaxPercent = 83f, GpaPoints = 3.0000f, CountsTowardGpa = true },
                new() { Id = Guid.NewGuid(), Letter = "B-", MinPercent = 76f, MaxPercent = 79f, GpaPoints = 2.6667f, CountsTowardGpa = true },
                new() { Id = Guid.NewGuid(), Letter = "C+", MinPercent = 73f, MaxPercent = 75f, GpaPoints = 2.3333f, CountsTowardGpa = true },
                new() { Id = Guid.NewGuid(), Letter = "C", MinPercent = 70f, MaxPercent = 72f, GpaPoints = 2.0000f, CountsTowardGpa = true },
                new() { Id = Guid.NewGuid(), Letter = "C-", MinPercent = 67f, MaxPercent = 69f, GpaPoints = 1.6667f, CountsTowardGpa = true },
                new() { Id = Guid.NewGuid(), Letter = "D+", MinPercent = 64f, MaxPercent = 66f, GpaPoints = 1.3333f, CountsTowardGpa = true },
                new() { Id = Guid.NewGuid(), Letter = "D", MinPercent = 60f, MaxPercent = 63f, GpaPoints = 1.0000f, CountsTowardGpa = true },
                new() { Id = Guid.NewGuid(), Letter = "F", MinPercent = 0f, MaxPercent = 59f, GpaPoints = 0.0000f, CountsTowardGpa = true },
                new() { Id = Guid.NewGuid(), Letter = "U", MinPercent = null, MaxPercent = null, GpaPoints = null, CountsTowardGpa = false },
                new() { Id = Guid.NewGuid(), Letter = "P", MinPercent = null, MaxPercent = null, GpaPoints = null, CountsTowardGpa = false },
                new() { Id = Guid.NewGuid(), Letter = "W", MinPercent = null, MaxPercent = null, GpaPoints = null, CountsTowardGpa = false },
                new() { Id = Guid.NewGuid(), Letter = "I", MinPercent = null, MaxPercent = null, GpaPoints = null, CountsTowardGpa = false }
            };

            await db.GradeScales.AddRangeAsync(scales);
            await db.SaveChangesAsync();
        }

        // 2. Seed Default Grading Policy (CourseId = null)
        var defaultPolicyExists = await db.GradingPolicies.AnyAsync(gp => gp.CourseId == null);
        if (!defaultPolicyExists)
        {
            var defaultPolicy = new GradingPolicy
            {
                Id = Guid.NewGuid(),
                CourseId = null,
                Week7Weight = 0.30f,
                Week12Weight = 0.20f,
                PrefinalWeight = 0.10f,
                FinalWeight = 0.40f
            };

            await db.GradingPolicies.AddAsync(defaultPolicy);
            await db.SaveChangesAsync();
        }

        // Ensure existing courses are active (since SQLite defaultValue for added bool column is false)
        var inactiveCourses = await db.Courses.Where(c => !c.IsActive).ToListAsync();
        if (inactiveCourses.Any())
        {
            foreach (var course in inactiveCourses)
            {
                course.IsActive = true;
            }
            await db.SaveChangesAsync();
        }

        // 3. Seed Department
        var dept = await db.Departments.FirstOrDefaultAsync(d => d.Name == "Computer Engineering");
        if (dept == null)
        {
            dept = new Department { Id = Guid.NewGuid(), Name = "Computer Engineering" };
            await db.Departments.AddAsync(dept);
            await db.SaveChangesAsync();
        }

        // 4. Seed Semester
        var semester = await db.Semesters.FirstOrDefaultAsync(s => s.Name == "Fall 2025/2026");
        if (semester == null)
        {
            semester = new Semester
            {
                Id = Guid.NewGuid(),
                Name = "Fall 2025/2026",
                StartDate = DateTime.UtcNow.AddMonths(-6),
                EndDate = DateTime.UtcNow.AddMonths(6),
                IsCurrent = true
            };
            await db.Semesters.AddAsync(semester);
            await db.SaveChangesAsync();
        }

        // 5. Seed RegistrationPeriod
        var regPeriodExists = await db.RegistrationPeriods.AnyAsync(rp => rp.SemesterId == semester.Id);
        if (!regPeriodExists)
        {
            var regPeriod = new RegistrationPeriod
            {
                Id = Guid.NewGuid(),
                SemesterId = semester.Id,
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow.AddMonths(1),
                IsOpen = true
            };
            await db.RegistrationPeriods.AddAsync(regPeriod);
            await db.SaveChangesAsync();
        }

        // 6. Seed Courses (if not already seeded)
        var deptId = dept.Id;
        var existingCourses = await db.Courses.ToListAsync();
        if (existingCourses.Any(c => c.Code.StartsWith("CS") || c.Name.Contains("Placeholder")))
        {
            db.AttendanceRecords.RemoveRange(await db.AttendanceRecords.ToListAsync());
            db.AttendanceSessions.RemoveRange(await db.AttendanceSessions.ToListAsync());
            db.Results.RemoveRange(await db.Results.ToListAsync());
            db.Enrollments.RemoveRange(await db.Enrollments.ToListAsync());
            db.Sections.RemoveRange(await db.Sections.ToListAsync());
            db.GradingPolicies.RemoveRange(await db.GradingPolicies.Where(gp => gp.CourseId != null).ToListAsync());

            db.Courses.RemoveRange(existingCourses);
            await db.SaveChangesAsync();
        }

        if (!await db.Courses.AnyAsync())
        {
            var courses = new List<Course>
            {
                // SEMESTER 1
                new Course { Id = Guid.NewGuid(), Code = "CC111", Name = "Intro to Computer", CreditHours = 3, SemesterNumber = 1, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "BA123", Name = "Mathematics I", CreditHours = 3, SemesterNumber = 1, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "BA113", Name = "Physics I", CreditHours = 3, SemesterNumber = 1, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "BA118", Name = "Chemistry", CreditHours = 3, SemesterNumber = 1, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "BA141", Name = "Engineering Mechanics I", CreditHours = 3, SemesterNumber = 1, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "LH131", Name = "ESP I", CreditHours = 3, SemesterNumber = 1, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "IM111", Name = "Industrial Relations", CreditHours = 3, SemesterNumber = 1, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "ME151", Name = "Engineering Drawing & Projection", CreditHours = 3, SemesterNumber = 1, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "IM112", Name = "Manufacturing Technology", CreditHours = 3, SemesterNumber = 1, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },

                // SEMESTER 2
                new Course { Id = Guid.NewGuid(), Code = "CC112", Name = "Structured Programming", CreditHours = 3, SemesterNumber = 2, PrerequisiteCode = "CC111", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "BA124", Name = "Mathematics II", CreditHours = 3, SemesterNumber = 2, PrerequisiteCode = "BA123", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "BA114", Name = "Physics II", CreditHours = 3, SemesterNumber = 2, PrerequisiteCode = "BA113", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "BA142", Name = "Engineering Mechanics II", CreditHours = 3, SemesterNumber = 2, PrerequisiteCode = "BA141", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "LH132", Name = "ESP II", CreditHours = 3, SemesterNumber = 2, PrerequisiteCode = "LH131", DepartmentId = deptId, IsActive = true },

                // SEMESTER 3
                new Course { Id = Guid.NewGuid(), Code = "CC212", Name = "Applied Programming", CreditHours = 3, SemesterNumber = 3, PrerequisiteCode = "CC112", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "BA223", Name = "Mathematics III", CreditHours = 3, SemesterNumber = 3, PrerequisiteCode = "BA124", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "EE231", Name = "Electrical Circuits I", CreditHours = 3, SemesterNumber = 3, PrerequisiteCode = "BA124", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC218", Name = "Discrete Mathematics", CreditHours = 3, SemesterNumber = 3, PrerequisiteCode = "CC111", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "LH231", Name = "Technical Report Writing", CreditHours = 3, SemesterNumber = 3, PrerequisiteCode = "LH132", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "NE264", Name = "Scientific Thinking", CreditHours = 3, SemesterNumber = 3, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },

                // SEMESTER 4
                new Course { Id = Guid.NewGuid(), Code = "CC213", Name = "Object-Oriented Programming", CreditHours = 3, SemesterNumber = 4, PrerequisiteCode = "CC212", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "BA224", Name = "Mathematics IV", CreditHours = 3, SemesterNumber = 4, PrerequisiteCode = "BA223", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "EE232", Name = "Electrical Circuits II", CreditHours = 3, SemesterNumber = 4, PrerequisiteCode = "EE231", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC215", Name = "Data Structures", CreditHours = 3, SemesterNumber = 4, PrerequisiteCode = "CC213 | CC212", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "EC238", Name = "Electronics I", CreditHours = 3, SemesterNumber = 4, PrerequisiteCode = "EE231", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC216", Name = "Digital Logic Design", CreditHours = 3, SemesterNumber = 4, PrerequisiteCode = "CC111", DepartmentId = deptId, IsActive = true },

                // SEMESTER 5
                new Course { Id = Guid.NewGuid(), Code = "CC319", Name = "Advanced Programming", CreditHours = 3, SemesterNumber = 5, PrerequisiteCode = "CC215", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "BA323", Name = "Mathematics V", CreditHours = 3, SemesterNumber = 5, PrerequisiteCode = "BA224", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "EC339", Name = "Electronics II", CreditHours = 3, SemesterNumber = 5, PrerequisiteCode = "EC238", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC317", Name = "Digital Systems Design", CreditHours = 3, SemesterNumber = 5, PrerequisiteCode = "CC216", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "EC320", Name = "Communication Theory", CreditHours = 3, SemesterNumber = 5, PrerequisiteCode = "BA224 & EE231", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "EE328", Name = "Electrical Power & Machines", CreditHours = 3, SemesterNumber = 5, PrerequisiteCode = "EE232", DepartmentId = deptId, IsActive = true },

                // SEMESTER 6
                new Course { Id = Guid.NewGuid(), Code = "CC316", Name = "Object-Oriented Programming", CreditHours = 3, SemesterNumber = 6, PrerequisiteCode = "CC319", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "BA326", Name = "Mathematics VI", CreditHours = 3, SemesterNumber = 6, PrerequisiteCode = "BA224", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC341", Name = "Digital Electronics", CreditHours = 3, SemesterNumber = 6, PrerequisiteCode = "EC238", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC331", Name = "Data & Computer Communications", CreditHours = 3, SemesterNumber = 6, PrerequisiteCode = "EC320", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC311", Name = "Computer Architecture", CreditHours = 3, SemesterNumber = 6, PrerequisiteCode = "CC317", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "NE364", Name = "Engineering Economy", CreditHours = 3, SemesterNumber = 6, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },

                // SEMESTER 7
                new Course { Id = Guid.NewGuid(), Code = "CC414", Name = "Database Systems", CreditHours = 3, SemesterNumber = 7, PrerequisiteCode = "CC319", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC410", Name = "Systems Programming", CreditHours = 3, SemesterNumber = 7, PrerequisiteCode = "CC319", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC421", Name = "Microprocessor Systems", CreditHours = 3, SemesterNumber = 7, PrerequisiteCode = "CC311", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC412", Name = "Computing Algorithms", CreditHours = 3, SemesterNumber = 7, PrerequisiteCode = "CC319", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC419", Name = "Numerical Methods", CreditHours = 3, SemesterNumber = 7, PrerequisiteCode = "CC112 & BA224", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC524", Name = "Neural Networks", CreditHours = 3, SemesterNumber = 7, PrerequisiteCode = "BA323 & CC112", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "EE418", Name = "Automatic Control Systems", CreditHours = 3, SemesterNumber = 7, PrerequisiteCode = "EE328 & BA323", DepartmentId = deptId, IsActive = true },

                // SEMESTER 8
                new Course { Id = Guid.NewGuid(), Code = "CC418", Name = "Operating Systems", CreditHours = 3, SemesterNumber = 8, PrerequisiteCode = "CC410", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC431", Name = "Computer Networks", CreditHours = 3, SemesterNumber = 8, PrerequisiteCode = "CC331", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC415", Name = "Data Acquisition Systems", CreditHours = 3, SemesterNumber = 8, PrerequisiteCode = "CC421", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC416", Name = "Computer Graphics", CreditHours = 3, SemesterNumber = 8, PrerequisiteCode = "CC319", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "IM400CC", Name = "Practical Training", CreditHours = 3, SemesterNumber = 8, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "IM423", Name = "Operations Research", CreditHours = 3, SemesterNumber = 8, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC511", Name = "Introduction to Artificial Intelligence", CreditHours = 3, SemesterNumber = 8, PrerequisiteCode = "CC218 & CC319", DepartmentId = deptId, IsActive = true },

                // SEMESTER 9
                new Course { Id = Guid.NewGuid(), Code = "CC501", Name = "Senior Project I", CreditHours = 3, SemesterNumber = 9, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC553", Name = "Mobile Applications", CreditHours = 3, SemesterNumber = 9, PrerequisiteCode = "CC316", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC552", Name = "Web Engineering", CreditHours = 3, SemesterNumber = 9, PrerequisiteCode = "CC212 | CC213", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC531", Name = "Advanced Networks", CreditHours = 3, SemesterNumber = 9, PrerequisiteCode = "CC431", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC528", Name = "Computer Systems Performance Analysis", CreditHours = 3, SemesterNumber = 9, PrerequisiteCode = null, DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC519", Name = "Introduction to Data Mining", CreditHours = 3, SemesterNumber = 9, PrerequisiteCode = "BA326 & CC511", DepartmentId = deptId, IsActive = true },

                // SEMESTER 10
                new Course { Id = Guid.NewGuid(), Code = "CC503", Name = "Senior Project II", CreditHours = 3, SemesterNumber = 10, PrerequisiteCode = "CC501", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC513", Name = "Computing Systems", CreditHours = 3, SemesterNumber = 10, PrerequisiteCode = "CC421 & CC418", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC515", Name = "Intro to Software Engineering", CreditHours = 3, SemesterNumber = 10, PrerequisiteCode = "CC319 & CC414", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC536", Name = "Cyber Security", CreditHours = 3, SemesterNumber = 10, PrerequisiteCode = "CC431 & CC418", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC532", Name = "Cloud Computing", CreditHours = 3, SemesterNumber = 10, PrerequisiteCode = "CC431 & CC414", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC510", Name = "Embedded Systems Design", CreditHours = 3, SemesterNumber = 10, PrerequisiteCode = "CC421 & CC418", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC514", Name = "Introduction to Big Data Management", CreditHours = 3, SemesterNumber = 10, PrerequisiteCode = "CC414 & CC418", DepartmentId = deptId, IsActive = true },
                new Course { Id = Guid.NewGuid(), Code = "CC525", Name = "Intelligent Robotics", CreditHours = 3, SemesterNumber = 10, PrerequisiteCode = "CC319 & EE418", DepartmentId = deptId, IsActive = true },
            };
            await db.Courses.AddRangeAsync(courses);
            await db.SaveChangesAsync();
        }

        var course1 = await db.Courses.FirstAsync(c => c.Code == "CC111");
        var course2 = await db.Courses.FirstAsync(c => c.Code == "CC112");
        await db.SaveChangesAsync();

        // 7. Seed Instructors
        var instructorKey1 = await ProvisionOrFallbackKeycloakUserAsync("instructor1@aast.edu", "Instructor One", "instructor", sp);
        var instructor1 = await db.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == instructorKey1);
        if (instructor1 == null)
        {
            instructor1 = new Instructor { Id = Guid.NewGuid(), KeycloakId = instructorKey1, FullName = "Instructor One", DepartmentId = dept.Id };
            await db.Instructors.AddAsync(instructor1);
        }
        await db.SaveChangesAsync();

        // 8. Seed 5 Demo Students
        var studentIds = new List<Guid>();
        for (int i = 1; i <= 5; i++)
        {
            var studentNumber = $"1910400{i}";
            var email = $"student{i}@aast.edu";
            var fullName = $"Student {i}";
            
            var keycloakId = await ProvisionOrFallbackKeycloakUserAsync(email, fullName, "student", sp);
            var student = await db.Students.FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);
            if (student == null)
            {
                student = new Student
                {
                    Id = Guid.NewGuid(),
                    KeycloakId = keycloakId,
                    StudentNumber = studentNumber,
                    FullName = fullName,
                    Email = email,
                    DateOfBirth = new DateOnly(2001, 1, i),
                    Phone = $"0100000000{i}",
                    Address = $"Cairo, Egypt, Street {i}",
                    DepartmentId = dept.Id,
                    YearLevel = 1
                };
                await db.Students.AddAsync(student);
            }
            else
            {
                student.Email = email;
                db.Students.Update(student);
            }
            await db.SaveChangesAsync();
            studentIds.Add(student.Id);
        }

        // 8b. Seed 3 Face Recognition Students
        var faceStudentsData = new[]
        {
            new { Number = "S001", Name = "Omar Samir", Key = "S001", Email = "s001@aast.edu" },
            new { Number = "S002", Name = "Omar Adel", Key = "S002", Email = "s002@aast.edu" },
            new { Number = "S003", Name = "Hazem Elfol", Key = "S003", Email = "s003@aast.edu" }
        };

        var faceStudentIds = new List<Guid>();
        foreach (var item in faceStudentsData)
        {
            var keycloakId = await ProvisionOrFallbackKeycloakUserAsync(item.Email, item.Name, "student", sp);
            var student = await db.Students.FirstOrDefaultAsync(s => s.StudentNumber == item.Number);
            if (student == null)
            {
                student = new Student
                {
                    Id = Guid.NewGuid(),
                    KeycloakId = keycloakId,
                    StudentNumber = item.Number,
                    FullName = item.Name,
                    Email = item.Email,
                    DateOfBirth = new DateOnly(2001, 1, 1),
                    Phone = "01000000000",
                    Address = "Cairo, Egypt",
                    DepartmentId = dept.Id,
                    YearLevel = 1,
                    FaceEncodingKey = item.Key
                };
                await db.Students.AddAsync(student);
            }
            else
            {
                student.Email = item.Email;
                student.FaceEncodingKey = item.Key;
                db.Students.Update(student);
            }
            await db.SaveChangesAsync();
            faceStudentIds.Add(student.Id);
        }

        // 9. Seed 4 Demo Sections
        var schedule1 = "[{\"day\":\"Sun\",\"startTime\":\"8:30 AM\",\"endTime\":\"10:00 AM\",\"room\":\"C201\"},{\"day\":\"Tue\",\"startTime\":\"8:30 AM\",\"endTime\":\"10:00 AM\",\"room\":\"C201\"}]";
        var schedule2 = "[{\"day\":\"Sun\",\"startTime\":\"12:30 PM\",\"endTime\":\"2:00 PM\",\"room\":\"C202\"},{\"day\":\"Tue\",\"startTime\":\"12:30 PM\",\"endTime\":\"2:00 PM\",\"room\":\"C202\"}]";
        var schedule3 = "[{\"day\":\"Mon\",\"startTime\":\"10:30 AM\",\"endTime\":\"12:00 PM\",\"room\":\"B101\"},{\"day\":\"Wed\",\"startTime\":\"10:30 AM\",\"endTime\":\"12:00 PM\",\"room\":\"B101\"}]";
        var schedule4 = "[{\"day\":\"Sat\",\"startTime\":\"2:30 PM\",\"endTime\":\"4:00 PM\",\"room\":\"A301\"},{\"day\":\"Mon\",\"startTime\":\"2:30 PM\",\"endTime\":\"4:00 PM\",\"room\":\"A301\"}]";
        
        var sections = await db.Sections.Where(s => s.SemesterId == semester.Id).ToListAsync();
        if (sections.Count < 4 || sections.Any(s => s.InstructorId != instructor1.Id))
        {
            // Clear dependent entities first to avoid FK constraints
            db.AttendanceRecords.RemoveRange(await db.AttendanceRecords.ToListAsync());
            db.AttendanceSessions.RemoveRange(await db.AttendanceSessions.ToListAsync());
            db.Results.RemoveRange(await db.Results.ToListAsync());
            db.Enrollments.RemoveRange(await db.Enrollments.ToListAsync());
            db.Sections.RemoveRange(sections);
            await db.SaveChangesAsync();

            var newSections = new List<Section>
            {
                new() { Id = Guid.NewGuid(), CourseId = course1.Id, InstructorId = instructor1.Id, SemesterId = semester.Id, ScheduleJson = schedule1, Capacity = 30, IsActive = true },
                new() { Id = Guid.NewGuid(), CourseId = course2.Id, InstructorId = instructor1.Id, SemesterId = semester.Id, ScheduleJson = schedule2, Capacity = 30, IsActive = true },
                new() { Id = Guid.NewGuid(), CourseId = course1.Id, InstructorId = instructor1.Id, SemesterId = semester.Id, ScheduleJson = schedule3, Capacity = 30, IsActive = true },
                new() { Id = Guid.NewGuid(), CourseId = course2.Id, InstructorId = instructor1.Id, SemesterId = semester.Id, ScheduleJson = schedule4, Capacity = 30, IsActive = true }
            };

            await db.Sections.AddRangeAsync(newSections);
            await db.SaveChangesAsync();
            sections = await db.Sections.Where(s => s.SemesterId == semester.Id).ToListAsync();
        }

        // 10. Enroll all 5 students in Section 1 and Section 2
        var section1 = sections.First(s => s.CourseId == course1.Id && s.InstructorId == instructor1.Id);
        var section2 = sections.First(s => s.CourseId == course2.Id && s.InstructorId == instructor1.Id);

        foreach (var studentId in studentIds)
        {
            var enrollExists1 = await db.Enrollments.AnyAsync(e => e.StudentId == studentId && e.SectionId == section1.Id);
            if (!enrollExists1)
            {
                var enroll = new Enrollment { Id = Guid.NewGuid(), StudentId = studentId, SectionId = section1.Id, WarningSent = false };
                await db.Enrollments.AddAsync(enroll);
            }

            var enrollExists2 = await db.Enrollments.AnyAsync(e => e.StudentId == studentId && e.SectionId == section2.Id);
            if (!enrollExists2)
            {
                var enroll = new Enrollment { Id = Guid.NewGuid(), StudentId = studentId, SectionId = section2.Id, WarningSent = false };
                await db.Enrollments.AddAsync(enroll);
            }
        }
        await db.SaveChangesAsync();

        // Enroll all 3 face students into Section 1 with FaceAttendanceEnabled = true
        foreach (var studentId in faceStudentIds)
        {
            var enrollExists = await db.Enrollments.AnyAsync(e => e.StudentId == studentId && e.SectionId == section1.Id);
            if (!enrollExists)
            {
                var enroll = new Enrollment
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    SectionId = section1.Id,
                    WarningSent = false,
                    FaceAttendanceEnabled = true
                };
                await db.Enrollments.AddAsync(enroll);
            }
            else
            {
                var enroll = await db.Enrollments.FirstAsync(e => e.StudentId == studentId && e.SectionId == section1.Id);
                enroll.FaceAttendanceEnabled = true;
                db.Enrollments.Update(enroll);
            }
        }
        await db.SaveChangesAsync();

        // 11. Seed past Attendance Sessions and Records for Section 1 and Section 2 (Disabled for clean week 1 testing)
        /*
        var enrolledSections = new[] { section1, section2 };
        foreach (var sec in enrolledSections)
        {
            var sessions = await db.AttendanceSessions.Where(s => s.SectionId == sec.Id).ToListAsync();
            if (sessions.Count < 3)
            {
                var enrollments = await db.Enrollments.Where(e => e.SectionId == sec.Id).ToListAsync();
                for (int sessionIndex = 1; sessionIndex <= 3; sessionIndex++)
                {
                    var startTime = DateTime.UtcNow.AddDays(-sessionIndex);
                    var session = new AttendanceSession
                    {
                        Id = Guid.NewGuid(),
                        SectionId = sec.Id,
                        InstructorId = sec.InstructorId,
                        StartTime = startTime,
                        EndTime = startTime.AddMinutes(90),
                        Method = "pin",
                        CurrentCode = "123456",
                        CodeExpiresAt = startTime.AddMinutes(90),
                        Lat = 30.0818,
                        Lng = 31.3235,
                        RadiusMeters = 50,
                        Week = sessionIndex
                    };
                    await db.AttendanceSessions.AddAsync(session);
                    await db.SaveChangesAsync();

                    // Create attendance records
                    foreach (var enroll in enrollments)
                    {
                        // Alternate present/absent status for demo data
                        var status = (enroll.StudentId.GetHashCode() + sessionIndex) % 3 == 0 ? "absent" : "present";
                        var record = new AttendanceRecord
                        {
                            Id = Guid.NewGuid(),
                            SessionId = session.Id,
                            StudentId = enroll.StudentId,
                            CheckedInAt = startTime.AddMinutes(10),
                            Status = status
                        };
                        await db.AttendanceRecords.AddAsync(record);
                    }
                    await db.SaveChangesAsync();
                }
            }
        }
        */

        // 12. Seed 1 published Result per enrollment
        var allEnrollments = await db.Enrollments
            .Include(e => e.Section)
            .ToListAsync();

        var scoreSets = new[]
        {
            new { w7 = 25f, w12 = 17f, wpf = 9f,  wf = 35f, total = 86f,   grade = "B+" },
            new { w7 = 20f, w12 = 14f, wpf = 6f,  wf = 32f, total = 72f,   grade = "C"  },
            new { w7 = 28f, w12 = 18f, wpf = 9f,  wf = 10f, total = 65f,   grade = "F"  }, // Failed by automatic F
            new { w7 = 15f, w12 = 10f, wpf = 4f,  wf = 20f, total = 49f,   grade = "F"  }, // Failed by low total
            new { w7 = 27f, w12 = 19f, wpf = 9f,  wf = 38f, total = 93f,   grade = "A"  },
            new { w7 = 22f, w12 = 15f, wpf = 7f,  wf = 30f, total = 74f,   grade = "C+" },
            new { w7 = 29f, w12 = 19f, wpf = 10f, wf = 39f, total = 97f,   grade = "A+" },
            new { w7 = 18f, w12 = 12f, wpf = 5f,  wf = 25f, total = 60f,   grade = "D"  },
            new { w7 = 24f, w12 = 16f, wpf = 8f,  wf = 33f, total = 81f,   grade = "B"  },
            new { w7 = 26f, w12 = 17f, wpf = 8f,  wf = 36f, total = 87f,   grade = "B+" }
        };

        for (int idx = 0; idx < allEnrollments.Count; idx++)
        {
            var enroll = allEnrollments[idx];
            var resultExists = await db.Results.AnyAsync(r => r.EnrollmentId == enroll.Id);
            if (!resultExists)
            {
                var set = scoreSets[idx % scoreSets.Length];
                var result = new Result
                {
                    Id = Guid.NewGuid(),
                    EnrollmentId = enroll.Id,
                    Week7Score = set.w7,
                    Week12Score = set.w12,
                    PrefinalScore = set.wpf,
                    FinalScore = set.wf,
                    TotalScore = set.total,
                    LetterGrade = set.grade,
                    Published = true,
                    IsManualOverride = false
                };
                await db.Results.AddAsync(result);
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task<string> ProvisionOrFallbackKeycloakUserAsync(
        string email, string fullName, string roleName, IServiceProvider? sp)
    {
        try
        {
            if (sp == null)
            {
                return Guid.NewGuid().ToString();
            }

            var config = sp.GetService<IConfiguration>();
            var httpClientFactory = sp.GetService<IHttpClientFactory>();

            if (config == null || httpClientFactory == null)
            {
                return Guid.NewGuid().ToString();
            }

            var tokenUrl = config["Keycloak:AdminApi:TokenUrl"];
            var baseUrl = config["Keycloak:AdminApi:BaseUrl"];
            var clientId = config["Keycloak:AdminApi:ClientId"] ?? "admin-cli";
            var clientSecret = config["Keycloak:AdminApi:ClientSecret"];

            if (string.IsNullOrEmpty(tokenUrl) || string.IsNullOrEmpty(baseUrl))
            {
                return Guid.NewGuid().ToString();
            }

            var client = httpClientFactory.CreateClient();
            var nvc = new List<KeyValuePair<string, string>>();
            if (clientId == "admin-cli" && (string.IsNullOrEmpty(clientSecret) || clientSecret == "mock_client_secret"))
            {
                nvc.Add(new("grant_type", "password"));
                nvc.Add(new("client_id", clientId));
                nvc.Add(new("username", "admin"));
                nvc.Add(new("password", "admin"));
            }
            else
            {
                nvc.Add(new("grant_type", "client_credentials"));
                nvc.Add(new("client_id", clientId));
                if (!string.IsNullOrEmpty(clientSecret))
                {
                    nvc.Add(new("client_secret", clientSecret));
                }
            }

            var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl) { Content = new FormUrlEncodedContent(nvc) };
            var res = await client.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                return Guid.NewGuid().ToString();
            }

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var token = doc.RootElement.GetProperty("access_token").GetString();
            if (string.IsNullOrEmpty(token)) return Guid.NewGuid().ToString();

            // Check if user already exists
            var getUserUrl = $"{baseUrl}/admin/realms/student-portal/users?email={Uri.EscapeDataString(email)}";
            var getReq = new HttpRequestMessage(HttpMethod.Get, getUserUrl);
            getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var getRes = await client.SendAsync(getReq);
            if (getRes.IsSuccessStatusCode)
            {
                var getJson = await getRes.Content.ReadAsStringAsync();
                using var getDoc = JsonDocument.Parse(getJson);
                if (getDoc.RootElement.ValueKind == JsonValueKind.Array && getDoc.RootElement.GetArrayLength() > 0)
                {
                    return getDoc.RootElement[0].GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
                }
            }

            // Create user
            var createUserUrl = $"{baseUrl}/admin/realms/student-portal/users";
            var keycloakUser = new
            {
                username = email,
                email = email,
                firstName = fullName,
                enabled = true,
                emailVerified = true,
                credentials = new[]
                {
                    new { type = "password", value = "TempPassword123!", temporary = true }
                },
                requiredActions = new[] { "UPDATE_PASSWORD" }
            };

            var postReq = new HttpRequestMessage(HttpMethod.Post, createUserUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(keycloakUser), System.Text.Encoding.UTF8, "application/json")
            };
            postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var postRes = await client.SendAsync(postReq);

            string? keycloakId = null;
            if (postRes.StatusCode == HttpStatusCode.Created)
            {
                var location = postRes.Headers.Location;
                keycloakId = location?.Segments.LastOrDefault()?.TrimEnd('/');
            }
            else if (postRes.StatusCode == HttpStatusCode.Conflict)
            {
                var fallbackRes = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, getUserUrl) { Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) } });
                if (fallbackRes.IsSuccessStatusCode)
                {
                    var fallbackJson = await fallbackRes.Content.ReadAsStringAsync();
                    using var fallbackDoc = JsonDocument.Parse(fallbackJson);
                    if (fallbackDoc.RootElement.ValueKind == JsonValueKind.Array && fallbackDoc.RootElement.GetArrayLength() > 0)
                    {
                        keycloakId = fallbackDoc.RootElement[0].GetProperty("id").GetString();
                    }
                }
            }

            if (string.IsNullOrEmpty(keycloakId))
            {
                return Guid.NewGuid().ToString();
            }

            // Fetch role representation
            var getRoleUrl = $"{baseUrl}/admin/realms/student-portal/roles/{roleName}";
            var roleReq = new HttpRequestMessage(HttpMethod.Get, getRoleUrl) { Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) } };
            var roleRes = await client.SendAsync(roleReq);
            if (roleRes.IsSuccessStatusCode)
            {
                var roleJson = await roleRes.Content.ReadAsStringAsync();
                using var roleDoc = JsonDocument.Parse(roleJson);

                // Assign role
                var assignRoleUrl = $"{baseUrl}/admin/realms/student-portal/users/{keycloakId}/role-mappings/realm";
                var roleArray = new[] { roleDoc.RootElement.Clone() };
                var assignReq = new HttpRequestMessage(HttpMethod.Post, assignRoleUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(roleArray), System.Text.Encoding.UTF8, "application/json"),
                    Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
                };
                await client.SendAsync(assignReq);
            }

            return keycloakId;
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }
}
