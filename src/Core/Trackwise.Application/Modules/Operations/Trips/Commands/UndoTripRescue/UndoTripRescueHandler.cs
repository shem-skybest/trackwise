using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class UndoTripRescueHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UndoTripRescueCommand, Result>
{
    public async Task<Result> Handle(UndoTripRescueCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);
        if (trip is null)
        {
            return Result.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        try
        {
            var rescue = trip.UndoRescue(req.RescueId);
            tenantUow.Repository<TripRescue>().Delete(rescue);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
