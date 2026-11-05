using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Common;

/// <summary>
/// Resolves the owning branch names for a page of branch-scoped master records, in one query.
/// <para>
/// Shared rather than copied per entity. Lazy loading is on, so reading <c>record.Branch</c> inside
/// a projection loop is a query per row - the N+1 this exists to avoid - and every
/// <c>IBranchScoped</c> list needs the identical batch. Ports was the first; vessels, commodities
/// and locations would have been the second through fourth copies.
/// </para>
/// </summary>
internal static class BranchNameLookup
{
    public static async Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        ITenantUnitOfWork tenantUow, IEnumerable<Guid?> branchIds, CancellationToken ct)
    {
        var ids = branchIds.OfType<Guid>().Distinct().ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        // Past the filter: a record scoped to a branch someone deleted should still name that
        // branch, rather than rendering as though it applied everywhere.
        return await tenantUow.Repository<Branch>().Query()
            .IgnoreQueryFilters()
            .Where(b => ids.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Name, ct);
    }
}
