using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Notifications;
using Trackwise.Application.Modules.Operations.CrewPlan.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Events;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Events;

/// <summary>
/// Tells the trip's own driver when a trip reaches their phone, moves to someone else, or is called
/// off. Crews rotate trucks, so this goes to <see cref="Trip.Driver" />, never a truck's main driver,
/// and only from dispatch on: the app's Today screen shows nothing earlier.
/// </summary>
internal sealed class TripDriverPushHandler(
    IPushNotificationService pushNotificationService,
    ICrewPlanService crewPlanService,
    ITenantUnitOfWork tenantUow,
    ILogger<TripDriverPushHandler> logger)
    : IDomainEventHandler<TripDispatchedEvent>,
        IDomainEventHandler<TripDriverChangedEvent>,
        IDomainEventHandler<TripCancelledEvent>
{
    public async Task Handle(TripDispatchedEvent @event, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(@event.TripId, ct);
        if (trip?.Driver is { } driver)
        {
            await PushAsync(driver, "Trip dispatched", Summary(trip), trip.Id);
        }
    }

    public async Task Handle(TripDriverChangedEvent @event, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(@event.TripId, ct);
        if (trip is null)
        {
            return;
        }

        if (trip.Driver is { } newDriver)
        {
            await PushAsync(newDriver, "Trip assigned to you", Summary(trip), trip.Id);
        }

        if (@event.OldDriverId is { } oldDriverId
            && await tenantUow.Repository<Employee>().GetByIdAsync(oldDriverId, ct) is { } oldDriver)
        {
            await PushAsync(oldDriver, "Trip moved to another driver", Summary(trip), trip.Id);
        }
    }

    public async Task Handle(TripCancelledEvent @event, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(@event.TripId, ct);
        if (trip is not null
            && @event.DriverId is { } driverId
            && await tenantUow.Repository<Employee>().GetByIdAsync(driverId, ct) is { } driver)
        {
            await PushAsync(driver, "Trip cancelled", Summary(trip), trip.Id);
        }
    }

    /// <summary>"Aug 29: Clearwater Catch, Inc., 4 drops" - the trip day, the client and the drop count.</summary>
    private string Summary(Trip trip)
    {
        var loads = trip.GetLoads().ToList();
        var day = crewPlanService.TripDay(loads);
        var clients = string.Join(", ", loads.Select(l => l.Customer.Name).Distinct());
        var drops = trip.Stops.Count(s => s.Type == TripStopType.DropOff);
        return $"{day:MMM d}: {clients}, {drops} {(drops == 1 ? "drop" : "drops")}";
    }

    private async Task PushAsync(Employee driver, string title, string body, Guid tripId)
    {
        if (string.IsNullOrEmpty(driver.DeviceToken))
        {
            logger.LogInformation("No device registered for driver {DriverId}; '{Title}' not pushed", driver.Id, title);
            return;
        }

        await pushNotificationService.SendNotificationAsync(
            title,
            body,
            driver.DeviceToken,
            new Dictionary<string, string> { ["tripId"] = tripId.ToString() });
    }
}
