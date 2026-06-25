using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Api.Data;
using StudentPortal.Api.Models.Entities;
using StudentPortal.Api.Services.Interfaces;
using FirebaseAdmin.Messaging;

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
        var notification = new StudentPortal.Api.Models.Entities.Notification
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
        List<DeviceToken> deviceTokens;
        try
        {
            deviceTokens = await _context.DeviceTokens.Where(t => t.StudentId == studentId).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch student device tokens.");
            return;
        }

        if (deviceTokens.Count == 0)
        {
            return;
        }

        try
        {
            var messaging = FirebaseMessaging.DefaultInstance;
            if (messaging == null)
            {
                _logger.LogWarning("Firebase not initialized or no device tokens");
                return;
            }

            var tokenStrings = deviceTokens.Select(t => t.FcmToken).ToList();

            var multicast = new MulticastMessage
            {
                Tokens = tokenStrings,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = title,
                    Body = body,
                },
                Data = new Dictionary<string, string>
                {
                    { "type", type },
                    { "notificationId", notification.Id.ToString() }
                }
            };

            var response = await messaging.SendEachForMulticastAsync(multicast);
            _logger.LogInformation(
                "FCM: {Success} delivered, {Fail} failed",
                response.SuccessCount, response.FailureCount);

            // Remove stale/invalid tokens
            for (int i = 0; i < response.Responses.Count; i++)
            {
                if (!response.Responses[i].IsSuccess)
                {
                    var code = response.Responses[i].Exception?.MessagingErrorCode;
                    if (code == MessagingErrorCode.Unregistered ||
                        code == MessagingErrorCode.InvalidArgument)
                    {
                        var stale = await _context.DeviceTokens
                            .FirstOrDefaultAsync(t => t.FcmToken == tokenStrings[i]);
                        if (stale != null)
                        {
                            _context.DeviceTokens.Remove(stale);
                            _logger.LogInformation("Removed stale FCM token");
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Notification DB row is already saved — push failure is non-fatal
            _logger.LogError(ex, "FCM send failed for student {Id}", studentId);
        }
    }
}
