using Microsoft.EntityFrameworkCore;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Financial.BillingBatches;

/// <summary>
/// Reads trips the way a billing batch sees them - one projection for the lot, then one lookup for
/// the booking references - because a half-month runs to forty trips and reading each trip's stops,
/// loads and truck through lazy loading would be hundreds of queries.
/// </summary>
internal static class BillableTripReader
{
    /// <summary>
    /// The client's completed trips in <paramref name="period" /> that no other batch has taken.
    /// </summary>
    public static async Task<IReadOnlyList<BillableTrip>> InPeriodAsync(
        ITenantUnitOfWork tenantUow, Guid customerId, BillingPeriod period, Guid? batchId, CancellationToken ct)
    {
        // An unpriced trip counts on the local day it completed; the UTC window is a day wider on
        // each side so the local-day filter below has every candidate to choose from.
        var completedFrom = period.Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(-1);
        var completedTo = period.End.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(2);

        var trips = Unbatched(tenantUow, batchId)
            .Where(t => t.Status == TripStatus.Completed)
            .Where(t => t.Stops.Any(s => s.Type == TripStopType.DropOff && s.Load.CustomerId == customerId))
            .Where(t => (t.RatedOn >= period.Start && t.RatedOn <= period.End)
                || (t.RatedOn == null && t.CompletedAt >= completedFrom && t.CompletedAt < completedTo));

        var read = await ReadAsync(tenantUow, trips, ct);
        return read.Where(t => period.Contains(t.BillingDate) && t.CustomerId == customerId).ToList();
    }

    /// <summary>The named trips as they stand now, whatever their status - to refresh a batch's lines.</summary>
    public static Task<IReadOnlyList<BillableTrip>> ByIdsAsync(
        ITenantUnitOfWork tenantUow, IReadOnlyCollection<Guid> tripIds, CancellationToken ct) =>
        ReadAsync(tenantUow, tenantUow.Repository<Trip>().Query().Where(t => tripIds.Contains(t.Id)), ct);

    /// <summary>
    /// Completed trips on no batch, for clients billed by batch: those with a billing cycle or a rate
    /// agreement. Anyone else's trips are billed some other way.
    /// </summary>
    public static Task<IReadOnlyList<BillableTrip>> UnbilledAsync(ITenantUnitOfWork tenantUow, CancellationToken ct)
    {
        var batchClients = tenantUow.Repository<Customer>().Query()
            .Where(c => c.BillingCycle != null)
            .Select(c => c.Id)
            .Union(tenantUow.Repository<RateAgreement>().Query().Select(a => a.CustomerId));

        var trips = Unbatched(tenantUow, null)
            .Where(t => t.Status == TripStatus.Completed)
            .Where(t => t.Stops.Any(s => s.Type == TripStopType.DropOff && batchClients.Contains(s.Load.CustomerId)));

        return ReadAsync(tenantUow, trips, ct);
    }

    private static IQueryable<Trip> Unbatched(ITenantUnitOfWork tenantUow, Guid? batchId)
    {
        var taken = tenantUow.Repository<BillingBatchTrip>().Query()
            .Where(l => l.BillingBatchId != batchId)
            .Select(l => l.TripId);

        return tenantUow.Repository<Trip>().Query().Where(t => !taken.Contains(t.Id));
    }

    private static async Task<IReadOnlyList<BillableTrip>> ReadAsync(
        ITenantUnitOfWork tenantUow, IQueryable<Trip> trips, CancellationToken ct)
    {
        var rows = await trips
            .Select(t => new
            {
                t.Id,
                t.Number,
                t.Status,
                t.RatedOn,
                t.RatedAt,
                t.CompletedAt,
                t.PriceOutdatedSince,
                TruckNumber = t.Truck != null ? t.Truck.Number : null,
                t.RateLineLabel,
                t.RateBaseAmount,
                t.RateFuelAmount,
                t.RateExtraDropAmount,
                Drops = t.Stops
                    .Where(s => s.Type == TripStopType.DropOff)
                    .Select(s => new
                    {
                        s.Load.CustomerId,
                        s.Load.Status,
                        s.Load.PlannedBoxes,
                        s.Load.ReturnedBoxes,
                        s.Load.ReturnedBags,
                        s.Load.BookingId
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        var bookingIds = rows.SelectMany(r => r.Drops).Select(d => d.BookingId).OfType<Guid>().Distinct().ToList();
        var bookings = await tenantUow.Repository<Booking>().Query()
            .Where(b => bookingIds.Contains(b.Id) && b.ReferenceNumber != null)
            .ToDictionaryAsync(b => b.Id, b => b.ReferenceNumber!, ct);

        var settings = tenantUow.GetCurrentTenant().Settings;

        return rows
            .Where(r => r.Drops.Count > 0)
            .Select(r =>
            {
                var references = r.Drops
                    .Select(d => d.BookingId is { } id ? bookings.GetValueOrDefault(id) : null)
                    .OfType<string>()
                    .Distinct()
                    .ToList();
                var boxes = r.Drops.Where(d => d.PlannedBoxes.HasValue).Sum(d => d.PlannedBoxes);

                return new BillableTrip(
                    r.Id,
                    r.Number,
                    r.Drops[0].CustomerId,
                    r.Status,
                    r.RatedOn ?? settings.LocalDate(r.CompletedAt ?? DateTime.UtcNow),
                    r.TruckNumber,
                    references.Count > 0 ? string.Join(", ", references) : null,
                    r.Drops.Count,
                    r.Drops.Count(d => d.Status == LoadStatus.FoulTrip),
                    r.Drops.Count(d => d.Status == LoadStatus.CancelledByClient),
                    r.Drops.Count(d => d.ReturnedBoxes > 0 || d.ReturnedBags > 0),
                    r.Drops.Any(d => d.PlannedBoxes.HasValue) ? boxes : null,
                    r.RateLineLabel,
                    r.RateBaseAmount,
                    r.RateFuelAmount,
                    r.RateExtraDropAmount,
                    r.RatedAt is null ? null : (r.RateBaseAmount ?? 0) + (r.RateFuelAmount ?? 0) + (r.RateExtraDropAmount ?? 0),
                    r.PriceOutdatedSince is { } since && r.RatedAt is { } rated && since > rated);
            })
            .ToList();
    }
}
