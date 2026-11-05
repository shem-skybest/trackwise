using Trackwise.Shared.Models;

namespace Trackwise.Infrastructure.Communications.SignalR.Clients;

/// <summary>
///     Hub client for receiving notifications.
/// </summary>
public interface INotificationHubClient
{
    Task ReceiveNotification(NotificationDto notification);
}
