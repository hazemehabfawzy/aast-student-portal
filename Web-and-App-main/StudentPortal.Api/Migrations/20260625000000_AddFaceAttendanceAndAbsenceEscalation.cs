using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StudentPortal.Api.Migrations
{
    public partial class AddFaceAttendanceAndAbsenceEscalation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaceEncodingKey",
                table: "Students",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FaceAttendanceEnabled",
                table: "Enrollments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWithdrawn",
                table: "Enrollments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "WithdrawnAt",
                table: "Enrollments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WithdrawalPending",
                table: "Enrollments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Method",
                table: "AttendanceRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: "pin");

            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "AttendanceRecords",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Week",
                table: "AttendanceSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FaceEncodingKey", table: "Students");
            migrationBuilder.DropColumn(name: "FaceAttendanceEnabled", table: "Enrollments");
            migrationBuilder.DropColumn(name: "IsWithdrawn", table: "Enrollments");
            migrationBuilder.DropColumn(name: "WithdrawnAt", table: "Enrollments");
            migrationBuilder.DropColumn(name: "WithdrawalPending", table: "Enrollments");
            migrationBuilder.DropColumn(name: "Method", table: "AttendanceRecords");
            migrationBuilder.DropColumn(name: "Confidence", table: "AttendanceRecords");
            migrationBuilder.DropColumn(name: "Week", table: "AttendanceSessions");
        }
    }
}
