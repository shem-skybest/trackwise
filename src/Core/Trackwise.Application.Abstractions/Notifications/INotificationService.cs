using Trackwise.Application.Abstractions.Notifications;
namespace Trackwise.Application.Abstractions.Notifications;

public interface INotificationService
{
    Task SendNotificationAsync(string title, string message);
}
