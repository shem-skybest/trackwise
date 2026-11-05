using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class MarkStopArrivedHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser,
    ILogger<MarkStopArrivedHandler> logger)
    : IAppRequestHandler<MarkStopArrivedCommand, Result>
{
    public async Task<Result> Handle(MarkStopArrivedCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);

        if (trip is null)
        {
            return Result.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        var stopIds = req.IncludeSameVisitPickups
            ? trip.GetSameVisitPickups(req.StopId)
                .Where(s => s.Id == req.StopId || !s.ArrivedAt.HasValue)
                .Select(s => s.Id)
                .ToList()
            : [req.StopId];

        try
        {
            foreach (var stopId in stopIds)
            {
                trip.MarkStopArrived(
                    stopId, req.TemperatureAtArrival, req.ArrivedAt?.ToUniversalTime(), currentUser.GetUserName());
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Marked {StopCount} stop(s) from '{StopId}' as arrived for trip '{TripName}' (ID: {TripId})",
            stopIds.Count, req.StopId, trip.Name, trip.Id);

        return Result.Ok();
    }
}
