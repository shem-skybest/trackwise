using Microsoft.EntityFrameworkCore;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches;

internal static class BillingBatchReader
{
    /// <summary>A batch with its trips and charges, in billing order.</summary>
    public static async Task<BillingBatchDto> ToDtoAsync(ITenantUnitOfWork tenantUow, BillingBatch batch, CancellationToken ct)
    {
        var customerName = await tenantUow.Repository<Customer>().Query()
            .Where(c => c.Id == batch.CustomerId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct);

        var trips = batch.Trips
            .OrderBy(t => t.BillingDate)
            .ThenBy(t => t.TripNumber)
            .Select(t => t.ToDto())
            .ToList();
        var tripNumbers = trips.ToDictionary(t => t.TripId, t => t.TripNumber);

        var charges = batch.Charges
            .OrderBy(c => c.CreatedAt)
            .Select(c => c.ToDto() with
            {
                TripNumber = c.TripId is { } tripId && tripNumbers.TryGetValue(tripId, out var number) ? number : null
            })
            .ToList();

        return batch.ToDto(customerName) with { Trips = trips, Charges = charges };
    }
}
