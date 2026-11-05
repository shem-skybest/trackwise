using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Events;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Notifications;

namespace Trackwise.Application.Modules.Operations.Loads.Events;

/// <summary>
/// Handles notifications when a load is assigned to a truck.
/// Sends push notification to driver and in-app notification to TMS portal users.
/// </summary>
internal sealed class LoadAssignedToTruckNotificationHandler(
    IPushNotificationService pushNotificationService,
    INotificationService notificationService,
    ITelegramNotificationService telegramNotificationService,
    ITenantUnitOfWork tenantUow,
    ILogger<LoadAssignedToTruckNotificationHandler> logger)
    : IDomainEventHandler<LoadAssignedToTruckEvent>
{
    public async Task Handle(LoadAssignedToTruckEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Load {LoadId} assigned to truck {TruckId}, sending notifications",
            @event.LoadId, @event.TruckId);

        var data = new Dictionary<string, string> { ["loadId"] = @event.LoadId.ToString() };

        // A load on a trip reaches its driver when the trip is dispatched (TripDriverPushHandler);
        // the truck's main drivers here may not be the crew driving it that day
        var onTrip = await tenantUow.Repository<TripStop>().Query()
            .AnyAsync(s => s.LoadId == @event.LoadId && s.Trip.Status != TripStatus.Cancelled, cancellationToken);

        // Send push notification to main driver
        if (!onTrip && !string.IsNullOrEmpty(@event.MainDriverDeviceToken))
        {
            await pushNotificationService.SendNotificationAsync(
                "Received a load",
                $"A new load #{@event.LoadNumber} has been assigned to you",
                @event.MainDriverDeviceToken,
                data);
        }

        // Send push notification to secondary driver
        if (!onTrip && !string.IsNullOrEmpty(@event.SecondaryDriverDeviceToken))
        {
            await pushNotificationService.SendNotificationAsync(
                "Received a load",
                $"A new load #{@event.LoadNumber} has been assigned to you",
                @event.SecondaryDriverDeviceToken,
                data);
        }

        // Send in-app notification for TMS portal users
        await notificationService.SendNotificationAsync(
            "Load assigned",
            $"Load #{@event.LoadNumber} has been assigned to {@event.DriverDisplayName}");

        // Send Telegram notification to drivers
        try
        {
            await telegramNotificationService.SendNotificationAsync(
                tenantUow.GetCurrentTenant().Id,
                "Load Assigned",
                $"Load #{@event.LoadNumber} has been assigned to {@event.DriverDisplayName}",
                TelegramChatRole.Driver,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Telegram notification for load assignment");
        }
    }
}
