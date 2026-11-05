using Trackwise.Application.Modules.Operations.Loads;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips;

/// <summary>
/// Who may record a trip event from the Driver App: a stop event goes to whoever may act on the
/// stop's load (<see cref="DriverLoadAccess" /> - the trip's driver, a rescue driver for the drops
/// they took over, or the office); a garage event only to the trip's own driver.
/// </summary>
internal static class DriverTripAccess
{
    public static async Task<Result> CheckAsync(
        ITenantUnitOfWork tenantUow, Guid? callerId, Trip trip, Guid? stopId, CancellationToken ct)
    {
        if (stopId is null)
        {
            return callerId is { } caller && trip.DriverId == caller
                ? Result.Ok()
                : Result.Fail("Only the trip's driver records its garage times.");
        }

        var stop = trip.Stops.FirstOrDefault(s => s.Id == stopId);
        if (stop is null)
        {
            return Result.Fail("That stop isn't on this trip.");
        }

        return await DriverLoadAccess.CanActOnAsync(tenantUow, callerId, stop.LoadId, ct)
            ? Result.Ok()
            : Result.Fail(DriverLoadAccess.NotYourLoad);
    }
}
