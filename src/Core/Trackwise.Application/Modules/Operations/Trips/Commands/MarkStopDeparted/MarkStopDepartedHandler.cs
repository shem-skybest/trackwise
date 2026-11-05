using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class MarkStopDepartedHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser,
    ILogger<MarkStopDepartedHandler> logger)
    : IAppRequestHandler<MarkStopDepartedCommand, Result>
{
    public async Task<Result> Handle(MarkStopDepartedCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);

        if (trip is null)
        {
            return Result.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        var departure = new StopDeparture(
            req.Outcome,
            req.TemperatureAtDeparture,
            req.DepartedAt?.ToUniversalTime(),
            req.HandlingStartedAt?.ToUniversalTime(),
            req.HandlingFinishedAt?.ToUniversalTime(),
            req.ReturnedBoxes,
            req.ReturnedBags,
            req.ReturnReason,
            string.IsNullOrWhiteSpace(req.OutcomeNote) ? null : req.OutcomeNote.Trim());

        var stopIds = req.IncludeSameVisitPickups
            ? trip.GetSameVisitPickups(req.StopId)
                .Where(s => s.Id == req.StopId || (s.ArrivedAt.HasValue && !s.DepartedAt.HasValue))
                .Select(s => s.Id)
                .ToList()
            : [req.StopId];

        try
        {
            foreach (var stopId in stopIds)
            {
                trip.MarkStopDeparted(stopId, departure, currentUser.GetUserName());
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Marked {StopCount} stop(s) from '{StopId}' as departed with outcome '{Outcome}' for trip '{TripName}' (ID: {TripId})",
            stopIds.Count, req.StopId, req.Outcome, trip.Name, trip.Id);

        return Result.Ok();
    }
}
