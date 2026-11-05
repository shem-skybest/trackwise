using Trackwise.Infrastructure.Communications.SignalR.Clients;
using Trackwise.Infrastructure.Communications.SignalR.Hubs;
using Trackwise.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Trackwise.Application.Abstractions.Realtime;

namespace Trackwise.Infrastructure.Communications.SignalR.Services;

/// <summary>
///     SignalR implementation of real-time notification service.
/// </summary>
internal sealed class SignalRNotificationService(IHubContext<NotificationHub, INotificationHubClient> hubContext)
    : IRealtimeNotificationService
{
    public async Task BroadcastNotificationAsync(
        string tenantId,
        NotificationDto notification,
        CancellationToken ct = default)
    {
        await hubContext.Clients
            .Group(tenantId)
            .ReceiveNotification(notification);
    }
}
