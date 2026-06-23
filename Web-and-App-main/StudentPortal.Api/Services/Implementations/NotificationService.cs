using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.Entities;
using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext context, 
        IHttpClientFactory httpClientFactory, 
        IConfiguration config, 
        ILogger<NotificationService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task RegisterDeviceTokenAsync(Guid studentId, string token, string platform)
    {
        var existing = await _context.DeviceTokens
            .FirstOrDefaultAsync(t => t.StudentId == studentId && t.Platform == platform);

        if (existing != null)
        {
            existing.FcmToken = token;
            existing.LastUsedAt = DateTime.UtcNow;
            _context.DeviceTokens.Update(existing);
        }
        else
        {
            var deviceToken = new DeviceToken
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                FcmToken = token,
                Platform = platform,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow
            };
            await _context.DeviceTokens.AddAsync(deviceToken);
        }

        await _context.SaveChangesAsync();
    }

    public async Task SendPushAsync(Guid studentId, string type, string title, string body)
    {
        // 1. Create a Notification row first (source of truth)
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            Type = type,
            Title = title,
            Body = body,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save notification record to database.");
            // Do not throw, keep executing push attempts
        }

        // 2. Fetch student's device tokens
        List<DeviceToken> tokens;
        try
        {
            tokens = await _context.DeviceTokens.Where(t => t.StudentId == studentId).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch student device tokens.");
            return;
        }

        if (tokens.Count == 0)
        {
            return;
        }

        var projectId = _config["Fcm:ProjectId"];
        var serverKey = _config["Fcm:ServerKey"];

        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(serverKey))
        {
            _logger.LogWarning("FCM Project ID or Server Key is not configured.");
            return;
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {serverKey}");

        foreach (var tokenObj in tokens)
        {
            try
            {
                var payload = new
                {
                    message = new
                    {
                        token = tokenObj.FcmToken,
                        notification = new
                        {
                            title = title,
                            body = body
                        },
                        data = new
                        {
                            type = type
                        }
                    }
                };

                var url = $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";
                var response = await client.PostAsJsonAsync(url, payload);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("FCM request failed for token {TokenId}. Status: {Status}, Body: {Body}", 
                        tokenObj.Id, response.StatusCode, responseBody);

                    if (responseBody.Contains("UNREGISTERED") || 
                        responseBody.Contains("INVALID_ARGUMENT") || 
                        response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                        response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        // Clean up stale token
                        _context.DeviceTokens.Remove(tokenObj);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Deleted stale FCM token {TokenId} from student {StudentId}", tokenObj.Id, studentId);
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch all FCM exceptions so it never bubbles up or affects the flow
                _logger.LogError(ex, "Exception occurred while sending FCM push notification to token {TokenId}", tokenObj.Id);
            }
        }
    }
}
