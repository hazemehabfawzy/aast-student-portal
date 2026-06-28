namespace StudentPortal.Api.Services.Interfaces;

public interface INotificationService
{
    Task SendPushAsync(Guid studentId, string type, string title, string body);
    Task RegisterDeviceTokenAsync(Guid studentId, string token, string platform);
}
