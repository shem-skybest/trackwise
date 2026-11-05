using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations;

/// <summary>
/// Batches the truck number a page of records displays, in one query.
/// <para>
/// Two separate reasons this cannot be a navigation property. Lazy loading is on, so reading one
/// per row is an N+1 the mapper hides; and trucks are soft-deletable, so the global query filter
/// would return null for a truck that has since been retired - blanking the truck on a trip,
/// maintenance record, DVIR or accident report that genuinely involved it. Display lookups
/// therefore read past the filter: what a record did reference is a fact about history, not a
/// record a steward is being offered.
/// </para>
/// </summary>
internal static class TruckRefResolver
{
    public static async Task<IReadOnlyDictionary<Guid, TruckRef>> ResolveAsync(
        ITenantUnitOfWork uow,
        IReadOnlyCollection<Guid> truckIds,
        CancellationToken ct = default)
    {
        if (truckIds.Count == 0)
        {
            return new Dictionary<Guid, TruckRef>();
        }

        return await uow.Repository<Truck>().Query()
            .IgnoreQueryFilters()
            .Where(t => truckIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => new TruckRef(t.Number), ct);
    }

    /// <summary>Single-id convenience wrapper for by-id handlers.</summary>
    public static async Task<string?> ResolveNumberAsync(
        ITenantUnitOfWork uow, Guid? truckId, CancellationToken ct = default)
    {
        if (truckId is not { } id)
        {
            return null;
        }

        var refs = await ResolveAsync(uow, [id], ct);
        return refs.TryGetValue(id, out var truckRef) ? truckRef.Number : null;
    }
}
