using Trackwise.Application.Abstractions.Notifications;
namespace Trackwise.Application.Abstractions.Notifications;

public interface IPushNotificationService
{
    Task SendNotificationAsync(string title, string body, string deviceToken, IReadOnlyDictionary<string, string>? data = null);
}
