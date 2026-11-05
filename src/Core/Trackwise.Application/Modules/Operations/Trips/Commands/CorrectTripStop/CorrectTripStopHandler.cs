using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class CorrectTripStopHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser,
    ILogger<CorrectTripStopHandler> logger)
    : IAppRequestHandler<CorrectTripStopCommand, Result>
{
    public async Task<Result> Handle(CorrectTripStopCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);
        if (trip is null)
        {
            return Result.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        var correction = new StopCorrection(
            req.ArrivedAt.ToUniversalTime(),
            req.TemperatureAtArrival,
            req.DepartedAt?.ToUniversalTime(),
            req.TemperatureAtDeparture,
            req.HandlingStartedAt?.ToUniversalTime(),
            req.HandlingFinishedAt?.ToUniversalTime(),
            req.Outcome,
            req.ReturnedBoxes,
            req.ReturnedBags,
            req.ReturnReason,
            req.OutcomeNote);

        IReadOnlyList<TripCorrection> changes;
        try
        {
            changes = trip.CorrectStop(req.StopId, correction, req.Reason, currentUser.GetUserName());
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await TripCorrectionWriter.AddAsync(tenantUow, changes, ct);
        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Corrected {FieldCount} value(s) on stop {StopId} of trip '{TripName}' (ID: {TripId})",
            changes.Count, req.StopId, trip.Name, trip.Id);
        return Result.Ok();
    }
}
