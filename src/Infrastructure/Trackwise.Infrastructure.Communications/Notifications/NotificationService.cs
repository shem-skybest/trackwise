using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Infrastructure.Communications.SignalR.Clients;
using Trackwise.Infrastructure.Communications.SignalR.Hubs;
using Trackwise.Mappings;
using Microsoft.AspNetCore.SignalR;
using Trackwise.Application.Abstractions.Notifications;

namespace Trackwise.Infrastructure.Communications.Services;

internal class NotificationService(
    ITenantUnitOfWork tenantUow,
    IHubContext<NotificationHub, INotificationHubClient> notificationHub)
    : INotificationService
{
    public async Task SendNotificationAsync(string title, string message)
    {
        var tenantId = tenantUow.GetCurrentTenant().Id;
        var notification = new Notification { Title = title, Message = message };

        var notificationRepository = tenantUow.Repository<Notification>();
        await notificationRepository.AddAsync(notification);
        var changes = await tenantUow.SaveChangesAsync();

        if (changes > 0)
        {
            await notificationHub.Clients.Group(tenantId.ToString())
                .ReceiveNotification(notification.ToDto());
        }
    }
}
