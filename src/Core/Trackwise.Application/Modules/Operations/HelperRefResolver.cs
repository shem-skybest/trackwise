using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations;

/// <summary>
/// Batches the helper name a page of trips displays, in one query - same reasoning as
/// <see cref="TruckRefResolver"/>: lazy loading makes a per-row read an N+1, and a helper is
/// soft-deletable, so the global query filter would blank a trip's helper once they leave.
/// </summary>
internal static class HelperRefResolver
{
    public static async Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        ITenantUnitOfWork uow,
        IReadOnlyCollection<Guid> helperIds,
        CancellationToken ct = default)
    {
        if (helperIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await uow.Repository<Helper>().Query()
            .IgnoreQueryFilters()
            .Where(h => helperIds.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id, h => h.Name, ct);
    }

    /// <summary>Single-id convenience wrapper for by-id handlers.</summary>
    public static async Task<string?> ResolveNameAsync(
        ITenantUnitOfWork uow, Guid? helperId, CancellationToken ct = default)
    {
        if (helperId is not { } id)
        {
            return null;
        }

        var refs = await ResolveAsync(uow, [id], ct);
        return refs.TryGetValue(id, out var name) ? name : null;
    }
}
