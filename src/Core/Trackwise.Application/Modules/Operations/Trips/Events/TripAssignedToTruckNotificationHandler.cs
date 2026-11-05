using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Events;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Notifications;

namespace Trackwise.Application.Modules.Operations.Trips.Events;

/// <summary>
/// Handles notifications when a trip is assigned to a truck.
/// Sends push notification to new driver, notification to old driver (if reassigned),
/// and in-app notification to TMS portal users.
/// </summary>
internal sealed class TripAssignedToTruckNotificationHandler(
    IPushNotificationService pushNotificationService,
    INotificationService notificationService,
    ITelegramNotificationService telegramNotificationService,
    ITenantUnitOfWork tenantUow,
    ILogger<TripAssignedToTruckNotificationHandler> logger)
    : IDomainEventHandler<TripAssignedToTruckEvent>
{
    public async Task Handle(TripAssignedToTruckEvent @event, CancellationToken cancellationToken)
    {
        var isReassignment = @event.OldTruckId.HasValue;

        logger.LogInformation(
            "Trip {TripId} {Action} truck {TruckId}, sending notifications",
            @event.TripId,
            isReassignment ? "reassigned to" : "assigned to",
            @event.NewTruckId);

        // A draft trip isn't on the driver's phone yet; dispatch pushes it (TripDriverPushHandler)
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(@event.TripId, cancellationToken);
        var onDriversPhone = trip is { Status: not TripStatus.Draft };

        // Send push notification to new driver
        if (onDriversPhone && !string.IsNullOrEmpty(@event.NewDriverDeviceToken))
        {
            await pushNotificationService.SendNotificationAsync(
                "New trip assigned",
                $"Trip #{@event.TripNumber} '{@event.TripName}' has been assigned to you",
                @event.NewDriverDeviceToken);
        }

        // If reassigned, notify the old driver
        if (onDriversPhone && isReassignment && !string.IsNullOrEmpty(@event.OldDriverDeviceToken))
        {
            await pushNotificationService.SendNotificationAsync(
                "Trip reassigned",
                $"Trip #{@event.TripNumber} '{@event.TripName}' has been reassigned to another truck",
                @event.OldDriverDeviceToken);
        }

        // Send in-app notification for TMS portal users
        var title = isReassignment ? "Trip reassigned" : "Trip assigned";
        await notificationService.SendNotificationAsync(
            title,
            $"Trip #{@event.TripNumber} has been assigned to {@event.NewDriverDisplayName}");

        // Send Telegram notification to drivers
        try
        {
            await telegramNotificationService.SendNotificationAsync(
                tenantUow.GetCurrentTenant().Id,
                title,
                $"Trip #{@event.TripNumber} '{@event.TripName}' has been assigned to {@event.NewDriverDisplayName}",
                TelegramChatRole.Driver,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Telegram notification for trip assignment");
        }
    }
}
