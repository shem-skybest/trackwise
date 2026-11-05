using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class RecordTripRescueGarageTimesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RecordTripRescueGarageTimesCommand, Result>
{
    public async Task<Result> Handle(RecordTripRescueGarageTimesCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);
        if (trip is null)
        {
            return Result.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        try
        {
            trip.RecordRescueGarageTimes(req.RescueId, req.DepartedAt?.ToUniversalTime(), req.ArrivedAt?.ToUniversalTime());
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
