using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Financial.BillingBatches;

internal static class BillingBatchSync
{
    /// <summary>
    /// Brings a draft's trip lines up to date and, with <paramref name="includeNew" />, adds the
    /// client's trips completed in the period since. New lines are added to the repository here.
    /// </summary>
    public static async Task SyncAsync(
        ITenantUnitOfWork tenantUow, BillingBatch batch, bool includeNew, CancellationToken ct)
    {
        var current = await CurrentTripsAsync(tenantUow, batch, ct);
        var trips = current.ToList();

        if (includeNew)
        {
            var known = trips.Select(t => t.TripId).ToHashSet();
            trips.AddRange((await BillableTripReader.InPeriodAsync(tenantUow, batch.CustomerId, batch.Period, batch.Id, ct))
                .Where(t => !known.Contains(t.TripId)));
        }

        foreach (var line in batch.SyncTrips(trips, includeNew))
        {
            await tenantUow.Repository<BillingBatchTrip>().AddAsync(line, ct);
        }
    }

    /// <summary>The batch's own trips as they stand now.</summary>
    public static Task<IReadOnlyList<BillableTrip>> CurrentTripsAsync(
        ITenantUnitOfWork tenantUow, BillingBatch batch, CancellationToken ct) =>
        BillableTripReader.ByIdsAsync(tenantUow, batch.Trips.Select(t => t.TripId).ToList(), ct);
}
