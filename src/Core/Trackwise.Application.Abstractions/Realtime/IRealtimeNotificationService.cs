using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.Realtime;

namespace Trackwise.Application.Abstractions.Realtime;

/// <summary>
///     Abstraction for real-time notifications.
/// </summary>
public interface IRealtimeNotificationService
{
    Task BroadcastNotificationAsync(
        string tenantId,
        NotificationDto notification,
        CancellationToken ct = default);
}
