using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Middleware;
using StudentPortal.Api.Services.Interfaces;
using StudentPortal.Api.Services.Implementations;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

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

    // Auto-sync Keycloak IDs on every startup
    // Retry sync up to 5 times with 5 second delays
    for (int i = 0; i < 5; i++)
    {
        try
        {
            await DbInitializer.SyncKeycloakIds(context, 
                app.Configuration,
                scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
            break;
        }
        catch (Exception ex)
        {
            if (i < 4)
            {
                Console.WriteLine($"Keycloak not ready, retry {i+1}/5... Error: {ex.Message}");
                await Task.Delay(5000);
            }
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

var serviceAccountPath = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "firebase-service-account.json"
);

if (File.Exists(serviceAccountPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(serviceAccountPath)
    });
    Console.WriteLine("✅ Firebase Admin SDK initialized");
}
else
{
    Console.WriteLine("⚠️ firebase-service-account.json not found - push disabled");
}

app.Run();
