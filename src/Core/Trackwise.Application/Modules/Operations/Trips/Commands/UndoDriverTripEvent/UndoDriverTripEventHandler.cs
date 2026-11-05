using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class UndoDriverTripEventHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser,
    ILogger<UndoDriverTripEventHandler> logger)
    : IAppRequestHandler<UndoDriverTripEventCommand, Result>
{
    public async Task<Result> Handle(UndoDriverTripEventCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);
        if (trip is null)
        {
            return Result.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        var access = await DriverTripAccess.CheckAsync(tenantUow, currentUser.GetUserId(), trip, req.StopId, ct);
        if (!access.IsSuccess)
        {
            return access;
        }

        try
        {
            trip.UndoCrewEvent(req.Kind, req.StopId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Driver app undid {Kind} on trip '{TripName}' (ID: {TripId}), stop {StopId}",
            req.Kind, trip.Name, trip.Id, req.StopId);
        return Result.Ok();
    }
}
