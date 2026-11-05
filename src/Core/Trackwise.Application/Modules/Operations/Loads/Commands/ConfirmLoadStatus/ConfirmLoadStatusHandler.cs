using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Abstractions.Notifications;
using Trackwise.Application.Modules.Operations.Trips.Commands;
using Trackwise.Domain.Primitives.Enums;
using MediatR;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

internal sealed class ConfirmLoadStatusHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    ISender sender)
    : IAppRequestHandler<ConfirmLoadStatusCommand, Result>
{
    public async Task<Result> Handle(ConfirmLoadStatusCommand req, CancellationToken ct)
    {
        var load = await tenantUow.Repository<Load>().GetByIdAsync(req.LoadId, ct);

        if (load is null)
        {
            return Result.Fail($"Could not find load with ID '{req.LoadId}'");
        }

        if (!await DriverLoadAccess.CanActOnAsync(tenantUow, currentUser.GetUserId(), load.Id, ct))
        {
            return Result.Fail(DriverLoadAccess.NotYourLoad);
        }

        var loadStatus = req.LoadStatus!.Value;

        // On a trip, picking up or delivering is the stop's arrival, so the crew report and the
        // trip's status see it - not just the load.
        if (TripStopFor(load, loadStatus) is { } stop)
        {
            return await sender.Send(new RecordDriverTripEventCommand
            {
                TripId = stop.TripId,
                StopId = stop.Id,
                Kind = TripEventKind.Arrived
            }, ct);
        }

        load.UpdateStatus(loadStatus, true);

        var changes = await tenantUow.SaveChangesAsync(ct);

        if (changes > 0)
        {
            await SendNotificationAsync(load);
        }

        return Result.Ok();
    }

    private static TripStop? TripStopFor(Load load, LoadStatus status)
    {
        var type = status switch
        {
            LoadStatus.PickedUp => TripStopType.PickUp,
            LoadStatus.Delivered => TripStopType.DropOff,
            _ => (TripStopType?)null
        };

        return type is null
            ? null
            : load.TripStops.FirstOrDefault(s => s.Type == type && s.Trip.Status != TripStatus.Cancelled);
    }

    private async Task SendNotificationAsync(Load load)
    {
        const string title = "Load updates";
        var driverName = load.AssignedTruck?.MainDriver?.GetFullName();
        var message = $"Driver {driverName} confirmed the load #{load.Number} status to '{load.Status}'";
        await notificationService.SendNotificationAsync(title, message);
    }
}
