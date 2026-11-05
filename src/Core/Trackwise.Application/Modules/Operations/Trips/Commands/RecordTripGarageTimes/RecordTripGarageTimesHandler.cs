using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class RecordTripGarageTimesHandler(
    ITenantUnitOfWork tenantUow,
    ILogger<RecordTripGarageTimesHandler> logger)
    : IAppRequestHandler<RecordTripGarageTimesCommand, Result>
{
    public async Task<Result> Handle(RecordTripGarageTimesCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);

        if (trip is null)
        {
            return Result.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        try
        {
            trip.RecordGarageTimes(req.DepartedAt?.ToUniversalTime(), req.ArrivedAt?.ToUniversalTime());
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation("Recorded garage times for trip '{TripName}' (ID: {TripId})", trip.Name, trip.Id);
        return Result.Ok();
    }
}
