using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Middleware;
using StudentPortal.Api.Services.Interfaces;
using StudentPortal.Api.Services.Implementations;

// Set QuestPDF license before any PDF generation
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Configure EF Core SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Keycloak Authentication
builder.Services.AddKeycloakAuth(builder.Configuration);

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("student"));
    options.AddPolicy("InstructorOnly", policy => policy.RequireRole("instructor"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

// Register services
builder.Services.AddScoped<IGradingService, GradingService>();
builder.Services.AddScoped<IGeofenceService, GeofenceService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IResultService, ResultService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<KeycloakAdminService>();
builder.Services.AddHttpClient<GradePredictionClient>();
builder.Services.AddScoped<IFaceRecognitionClient, FaceRecognitionClient>();
builder.Services.AddHostedService<AttendanceWarningJob>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Prevent circular reference errors when serializing EF Core entity graphs
        // (e.g. Section → Course → Department → Sections...)
        options.JsonSerializerOptions.ReferenceHandler = 
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = 
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Student Portal API", Version = "v1" });
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // EnsureCreated handles the case where no migrations exist yet
    context.Database.EnsureCreated();
    // Run any pending migrations on top
    try { context.Database.Migrate(); } catch { /* already up to date or no migrations */ }
    await DbInitializer.SeedAsync(context, scope.ServiceProvider);
}

// Sync Keycloak UUIDs into DB on every startup — prevents UUID drift after Docker restarts
// Retries up to 5 times with 5s delay to allow Keycloak to finish booting
for (int attempt = 0; attempt < 5; attempt++)
{
    try
    {
        using var syncScope = app.Services.CreateScope();
        var db     = syncScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = app.Configuration;

        var tokenUrl = config["Keycloak:AdminApi:TokenUrl"]
            ?? $"{config["Keycloak:AdminApi:BaseUrl"]}/realms/master/protocol/openid-connect/token";
        var baseUrl  = config["Keycloak:AdminApi:BaseUrl"] ?? "http://keycloak:8080";

        using var http = new System.Net.Http.HttpClient();

        // Get admin token
        var form = new System.Net.Http.FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"]  = "admin-cli",
            ["username"]   = "admin",
            ["password"]   = "admin",
        });
        var tokenResp = await http.PostAsync(tokenUrl, form);
        tokenResp.EnsureSuccessStatusCode();
        var tokenJson  = System.Text.Json.JsonDocument.Parse(await tokenResp.Content.ReadAsStringAsync());
        var adminToken = tokenJson.RootElement.GetProperty("access_token").GetString()!;

        // Get all realm users
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        var usersResp = await http.GetAsync($"{baseUrl}/admin/realms/student-portal/users?max=100");
        usersResp.EnsureSuccessStatusCode();
        var usersJson = System.Text.Json.JsonDocument.Parse(await usersResp.Content.ReadAsStringAsync());

        int syncCount = 0;
        foreach (var user in usersJson.RootElement.EnumerateArray())
        {
            var kcId    = user.GetProperty("id").GetString()!;
            var email   = user.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";

            // Match Students by Email (Student entity has Email column)
            if (!string.IsNullOrEmpty(email))
            {
                var student = db.Students.FirstOrDefault(s => s.Email != null && s.Email.ToLower() == email.ToLower());
                if (student != null && student.KeycloakId != kcId)
                {
                    student.KeycloakId = kcId;
                    syncCount++;
                    continue;
                }
            }

            // Match Instructors by username pattern (Instructor entity has no Email column)
            var username = user.TryGetProperty("username", out var un) ? un.GetString() ?? "" : "";
            if (!string.IsNullOrEmpty(username))
            {
                // "instructor.one" -> "Instructor One"
                var namePart = string.Join(" ", username.Split('.').Select(w =>
                    char.ToUpper(w[0]) + w.Substring(1))).ToLower();
                var instructor = db.Instructors.AsEnumerable()
                    .FirstOrDefault(i => i.FullName.ToLower().Contains(namePart));
                if (instructor != null && instructor.KeycloakId != kcId)
                {
                    instructor.KeycloakId = kcId;
                    syncCount++;
                }
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"Keycloak ID sync complete: {syncCount} record(s) updated.");
        break;
    }
    catch (Exception ex)
    {
        if (attempt < 4)
        {
            Console.WriteLine($"Keycloak sync retry {attempt + 1}/5... ({ex.Message})");
            await Task.Delay(5000);
        }
        else
        {
            Console.WriteLine($"Keycloak sync skipped after 5 attempts: {ex.Message}");
        }
    }
}

// Register global exception handling middleware first
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
