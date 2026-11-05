using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Common;

/// <summary>
/// Walks a location's chain of parent zones, so a write can refuse to close the chain into a loop.
/// <para>
/// A cycle is not a cosmetic problem: the dependency probe, any future zone rollup and the UI tree
/// would all follow the chain, and a loop hangs whichever reaches it first. Refusing the write is
/// cheaper than defending every reader against it.
/// </para>
/// </summary>
internal static class LocationAncestry
{
    /// <summary>
    /// A zone nested ten deep is already a modelling mistake, and the cap is what stops a cycle
    /// that pre-dates this check from hanging the walk that was meant to detect it.
    /// </summary>
    private const int MaxDepth = 10;

    /// <summary>
    /// Whether <paramref name="candidateParentId"/> sits at or beneath <paramref name="locationId"/>,
    /// which is what would make it an illegal parent.
    /// </summary>
    public static async Task<bool> WouldCycleAsync(
        ITenantUnitOfWork tenantUow,
        Guid locationId,
        Guid candidateParentId,
        CancellationToken ct)
    {
        if (candidateParentId == locationId)
        {
            return true;
        }

        var current = candidateParentId;

        for (var hop = 0; hop < MaxDepth; hop++)
        {
            // Past the filter deliberately: a deleted zone still holds its parent link, and
            // restoring it later would reinstate a cycle this check had declared impossible.
            var parent = await tenantUow.Repository<Location>().Query()
                .IgnoreQueryFilters()
                .Where(l => l.Id == current)
                .Select(l => l.ParentLocationId)
                .FirstOrDefaultAsync(ct);

            if (parent is null)
            {
                return false;
            }

            if (parent == locationId)
            {
                return true;
            }

            current = parent.Value;
        }

        // Depth exhausted without reaching the top. Something is already looping, so treat the new
        // link as a cycle rather than adding to one.
        return true;
    }
}
