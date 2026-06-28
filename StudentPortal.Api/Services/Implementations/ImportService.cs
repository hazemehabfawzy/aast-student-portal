using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.DTOs.Responses;
using StudentPortal.Api.Models.Entities;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Services.Implementations;

public class ImportService : IImportService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ImportService> _logger;

    public ImportService(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<ImportService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<BulkImportResponse> ImportStudentsAsync(Stream fileStream, string contentType)
    {
        var records = await ParseFileAsync(fileStream, contentType);
        var response = new BulkImportResponse { TotalRows = records.Count };

        if (records.Count == 0) return response;

        var (adminToken, roleRep) = await PrepareKeycloakDataAsync("student");

        int rowNumber = 1; // 1-indexed (header is row 1, data starts at row 2 or we can just count lines)
        foreach (var record in records)
        {
            rowNumber++;
            // 1. Validate fields
            var studentNumber = record.GetValueOrDefault("studentnumber", string.Empty);
            var fullName = record.GetValueOrDefault("fullname", string.Empty);
            var email = record.GetValueOrDefault("email", string.Empty);
            var deptName = record.GetValueOrDefault("departmentname", string.Empty);

            if (string.IsNullOrWhiteSpace(studentNumber) ||
                string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(deptName))
            {
                response.Failed++;
                response.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNumber,
                    StudentNumber = studentNumber,
                    Reason = "Missing required fields (student_number, full_name, email, department_name)."
                });
                continue;
            }

            if (!IsValidEmail(email))
            {
                response.Failed++;
                response.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNumber,
                    StudentNumber = studentNumber,
                    Reason = $"Invalid email format: {email}"
                });
                continue;
            }

            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Name.ToLower() == deptName.ToLower());
            if (department == null)
            {
                response.Failed++;
                response.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNumber,
                    StudentNumber = studentNumber,
                    Reason = $"Department does not exist: {deptName}"
                });
                continue;
            }

            // 2. Check if student already exists locally
            var existingStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);
            if (existingStudent != null)
            {
                try
                {
                    existingStudent.FullName = fullName;
                    existingStudent.Email = email;
                    existingStudent.Phone = record.GetValueOrDefault("phone", string.Empty);
                    existingStudent.Address = record.GetValueOrDefault("address", string.Empty);
                    existingStudent.DepartmentId = department.Id;

                    if (record.TryGetValue("yearlevel", out var yStr) && int.TryParse(yStr, out var yearLevel))
                    {
                        existingStudent.YearLevel = yearLevel;
                    }

                    _context.Students.Update(existingStudent);
                    await _context.SaveChangesAsync();

                    response.Succeeded++;
                }
                catch (Exception ex)
                {
                    response.Failed++;
                    response.Errors.Add(new BulkImportError
                    {
                        RowNumber = rowNumber,
                        StudentNumber = studentNumber,
                        Reason = $"Failed to update local student: {ex.Message}"
                    });
                }
                continue;
            }

            // 3. Keycloak Integration
            string? keycloakId = null;
            try
            {
                keycloakId = await ProvisionKeycloakUserAsync(email, fullName, "student", adminToken, roleRep);
            }
            catch (Exception ex)
            {
                response.Failed++;
                response.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNumber,
                    StudentNumber = studentNumber,
                    Reason = $"Keycloak creation failed: {ex.Message}"
                });
                continue;
            }

            if (string.IsNullOrEmpty(keycloakId))
            {
                response.Failed++;
                response.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNumber,
                    StudentNumber = studentNumber,
                    Reason = "Failed to obtain Keycloak ID."
                });
                continue;
            }

            // 4. Create local Student row
            try
            {
                var newStudent = new Student
                {
                    Id = Guid.NewGuid(),
                    KeycloakId = keycloakId,
                    StudentNumber = studentNumber,
                    FullName = fullName,
                    Email = email,
                    Phone = record.GetValueOrDefault("phone", string.Empty),
                    Address = record.GetValueOrDefault("address", string.Empty),
                    DepartmentId = department.Id,
                    YearLevel = record.TryGetValue("yearlevel", out var yStr) && int.TryParse(yStr, out var y) ? y : 1
                };

                await _context.Students.AddAsync(newStudent);
                await _context.SaveChangesAsync();

                response.Succeeded++;
            }
            catch (Exception ex)
            {
                _logger.LogError("ORPHANED_KEYCLOAK_USER: studentNumber={StudentNumber} keycloakId={KeycloakId}", studentNumber, keycloakId);
                response.Failed++;
                response.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNumber,
                    StudentNumber = studentNumber,
                    Reason = $"Local DB insertion failed (Orphaned Keycloak User registered): {ex.Message}"
                });
            }
        }

        return response;
    }

    public async Task<BulkImportResponse> ImportInstructorsAsync(Stream fileStream, string contentType)
    {
        var records = await ParseFileAsync(fileStream, contentType);
        var response = new BulkImportResponse { TotalRows = records.Count };

        if (records.Count == 0) return response;

        var (adminToken, roleRep) = await PrepareKeycloakDataAsync("instructor");

        int rowNumber = 1;
        foreach (var record in records)
        {
            rowNumber++;
            // For instructors, we'll support both "instructor_number" or "student_number" columns
            var identifier = record.GetValueOrDefault("instructornumber", record.GetValueOrDefault("studentnumber", string.Empty));
            var fullName = record.GetValueOrDefault("fullname", string.Empty);
            var email = record.GetValueOrDefault("email", string.Empty);
            var deptName = record.GetValueOrDefault("departmentname", string.Empty);

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(deptName))
            {
                response.Failed++;
                response.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNumber,
                    StudentNumber = identifier,
                    Reason = "Missing required fields (full_name, email, department_name)."
                });
                continue;
            }

            if (!IsValidEmail(email))
            {
                response.Failed++;
                response.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNumber,
                    StudentNumber = identifier,
                    Reason = $"Invalid email format: {email}"
                });
                continue;
            }

            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Name.ToLower() == deptName.ToLower());
            if (department == null)
            {
                response.Failed++;
                response.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNumber,
                    StudentNumber = identifier,
                    Reason = $"Department does not exist: {deptName}"
                });
                continue;
            }

            // Check if Instructor already exists locally.
            // First we check in Keycloak by email to see if there is an existing user.
            // If they exist in Keycloak, we get their Keycloak ID and check the local database.
            string? keycloakId = null;
            try
            {
                keycloakId = await FetchKeycloakUserIdByEmailAsync(email, adminToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to check existing user in Keycloak for {Email}: {Message}", email, ex.Message);
            }

            if (!string.IsNullOrEmpty(keycloakId))
            {
                var existingInstructor = await _context.Instructors.FirstOrDefaultAsync(i => i.KeycloakId == keycloakId);
                if (existingInstructor != null)
                {
                    try
                    {
                        existingInstructor.FullName = fullName;
                        existingInstructor.DepartmentId = department.Id;

                        _context.Instructors.Update(existingInstructor);
                        await _context.SaveChangesAsync();

                        response.Succeeded++;
                    }
                    catch (Exception ex)
                    {
                        response.Failed++;
                        response.Errors.Add(new BulkImportError
                        {
                            RowNumber = rowNumber,
                            StudentNumber = identifier,
                            Reason = $"Failed to update local instructor: {ex.Message}"
                        });
                    }
                    continue;
                }
            }

            // Keycloak creation (if they didn't exist or if they exist in Keycloak but not locally)
            if (string.IsNullOrEmpty(keycloakId))
            {
                try
                {
                    keycloakId = await ProvisionKeycloakUserAsync(email, fullName, "instructor", adminToken, roleRep);
                }
                catch (Exception ex)
                {
                    response.Failed++;
                    response.Errors.Add(new BulkImportError
                    {
                        RowNumber = rowNumber,
                        StudentNumber = identifier,
                        Reason = $"Keycloak creation failed: {ex.Message}"
                    });
                    continue;
                }
            }
            else
            {
                // Ensure the instructor role is assigned if the user existed in Keycloak but not locally
                try
                {
                    await AssignRoleToUserAsync(keycloakId, roleRep, adminToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to ensure role assignment for Keycloak user {KeycloakId}: {Message}", keycloakId, ex.Message);
                }
            }

            if (string.IsNullOrEmpty(keycloakId))
            {
                response.Failed++;
                response.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNumber,
                    StudentNumber = identifier,
                    Reason = "Failed to obtain Keycloak ID."
                });
                continue;
            }

            // Create local Instructor row
            try
            {
                var newInstructor = new Instructor
                {
                    Id = Guid.NewGuid(),
                    KeycloakId = keycloakId,
                    FullName = fullName,
                    DepartmentId = department.Id
                };

                await _context.Instructors.AddAsync(newInstructor);
                await _context.SaveChangesAsync();

                response.Succeeded++;
            }
            catch (Exception ex)
            {
                _logger.LogError("ORPHANED_KEYCLOAK_USER: studentNumber={Identifier} keycloakId={KeycloakId}", identifier, keycloakId);
                response.Failed++;
                response.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNumber,
                    StudentNumber = identifier,
                    Reason = $"Local DB insertion failed (Orphaned Keycloak User registered): {ex.Message}"
                });
            }
        }

        return response;
    }

    private async Task<(string Token, JsonElement RoleRep)> PrepareKeycloakDataAsync(string roleName)
    {
        var token = await GetAdminTokenAsync();
        var roleRep = await FetchRoleRepresentationAsync(roleName, token);
        return (token, roleRep);
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var client = _httpClientFactory.CreateClient();
        var tokenUrl = _config["Keycloak:AdminApi:TokenUrl"] ?? throw new InvalidOperationException("Keycloak:AdminApi:TokenUrl is not configured.");
        var clientId = _config["Keycloak:AdminApi:ClientId"] ?? "admin-cli";
        var clientSecret = _config["Keycloak:AdminApi:ClientSecret"] ?? string.Empty;

        var nvc = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", clientId)
        };
        if (!string.IsNullOrEmpty(clientSecret))
        {
            nvc.Add(new("client_secret", clientSecret));
        }

        var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl) { Content = new FormUrlEncodedContent(nvc) };
        var res = await client.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            var content = await res.Content.ReadAsStringAsync();
            throw new Exception($"Failed to obtain Keycloak admin token: Status {res.StatusCode}, Content: {content}");
        }

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString() ?? throw new Exception("Token missing access_token field.");
    }

    private async Task<JsonElement> FetchRoleRepresentationAsync(string roleName, string token)
    {
        var client = _httpClientFactory.CreateClient();
        var baseUrl = _config["Keycloak:AdminApi:BaseUrl"] ?? "http://localhost:8080";
        var getRoleUrl = $"{baseUrl}/admin/realms/student-portal/roles/{roleName}";

        var req = new HttpRequestMessage(HttpMethod.Get, getRoleUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            var content = await res.Content.ReadAsStringAsync();
            throw new Exception($"Failed to fetch role '{roleName}' representation: Status {res.StatusCode}, Content: {content}");
        }

        var json = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private async Task<string?> ProvisionKeycloakUserAsync(string email, string fullName, string roleName, string adminToken, JsonElement roleRep)
    {
        var client = _httpClientFactory.CreateClient();
        var baseUrl = _config["Keycloak:AdminApi:BaseUrl"] ?? "http://localhost:8080";
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
                new
                {
                    type = "password",
                    value = "TempPassword123!",
                    temporary = true
                }
            },
            requiredActions = new[] { "UPDATE_PASSWORD" }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, createUserUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(keycloakUser), System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var res = await client.SendAsync(req);
        string? keycloakId = null;

        if (res.StatusCode == HttpStatusCode.Created)
        {
            var location = res.Headers.Location;
            keycloakId = location?.Segments.LastOrDefault()?.TrimEnd('/');
        }
        else if (res.StatusCode == HttpStatusCode.Conflict)
        {
            // Fetch the existing user by email
            keycloakId = await FetchKeycloakUserIdByEmailAsync(email, adminToken);
        }
        else
        {
            var content = await res.Content.ReadAsStringAsync();
            throw new Exception($"Keycloak creation status: {res.StatusCode}, Content: {content}");
        }

        if (string.IsNullOrEmpty(keycloakId))
        {
            throw new Exception("Could not retrieve Keycloak ID for the user.");
        }

        // Assign Role
        await AssignRoleToUserAsync(keycloakId, roleRep, adminToken);

        return keycloakId;
    }

    private async Task<string?> FetchKeycloakUserIdByEmailAsync(string email, string token)
    {
        var client = _httpClientFactory.CreateClient();
        var baseUrl = _config["Keycloak:AdminApi:BaseUrl"] ?? "http://localhost:8080";
        var getUserUrl = $"{baseUrl}/admin/realms/student-portal/users?email={Uri.EscapeDataString(email)}";

        var req = new HttpRequestMessage(HttpMethod.Get, getUserUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            var content = await res.Content.ReadAsStringAsync();
            throw new Exception($"Failed to fetch Keycloak user by email: Status {res.StatusCode}, Content: {content}");
        }

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
        {
            var user = doc.RootElement[0];
            return user.GetProperty("id").GetString();
        }

        return null;
    }

    private async Task AssignRoleToUserAsync(string keycloakId, JsonElement roleRep, string token)
    {
        var client = _httpClientFactory.CreateClient();
        var baseUrl = _config["Keycloak:AdminApi:BaseUrl"] ?? "http://localhost:8080";
        var assignRoleUrl = $"{baseUrl}/admin/realms/student-portal/users/{keycloakId}/role-mappings/realm";

        // Wrap the role representation inside an array
        var roleArray = new[] { roleRep };
        var req = new HttpRequestMessage(HttpMethod.Post, assignRoleUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(roleArray), System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            var content = await res.Content.ReadAsStringAsync();
            throw new Exception($"Failed to assign realm role to user {keycloakId}: Status {res.StatusCode}, Content: {content}");
        }
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<Dictionary<string, string>>> ParseFileAsync(Stream fileStream, string contentType)
    {
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        ms.Position = 0;

        if (contentType.Contains("openxmlformats-officedocument.spreadsheetml.sheet", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("excel", StringComparison.OrdinalIgnoreCase))
        {
            return ParseExcel(ms);
        }
        else
        {
            return ParseCsv(ms);
        }
    }

    private List<Dictionary<string, string>> ParseExcel(Stream stream)
    {
        var records = new List<Dictionary<string, string>>();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return records;

        var range = worksheet.RangeUsed();
        if (range == null) return records;

        var rows = range.RowsUsed().ToList();
        if (rows.Count <= 1) return records;

        var headerRow = rows[0];
        var headers = headerRow.Cells().Select(c => c.Value.ToString().Trim().ToLowerInvariant().Replace("_", "")).ToList();

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int colIndex = 0; colIndex < headers.Count; colIndex++)
            {
                var header = headers[colIndex];
                if (string.IsNullOrEmpty(header)) continue;
                var cellValue = row.Cell(colIndex + 1).Value.ToString().Trim();
                record[header] = cellValue;
            }
            records.Add(record);
        }
        return records;
    }

    private List<Dictionary<string, string>> ParseCsv(Stream stream)
    {
        var records = new List<Dictionary<string, string>>();
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, -1, leaveOpen: true);

        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine)) return records;

        var headers = ParseCsvLine(headerLine);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = ParseCsvLine(line);
            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i].Trim().ToLowerInvariant().Replace("_", "");
                var val = i < values.Count ? values[i] : string.Empty;
                record[header] = val.Trim();
            }
            records.Add(record);
        }
        return records;
    }

    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result;
    }
}
