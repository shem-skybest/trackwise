using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Common;

/// <summary>
/// Resolves customer names for a page of master records in one query - the customer-side twin of
/// <see cref="BranchNameLookup"/>, for the same N+1 reason.
/// </summary>
internal static class CustomerNameLookup
{
    public static async Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        ITenantUnitOfWork tenantUow, IEnumerable<Guid?> customerIds, CancellationToken ct)
    {
        var ids = customerIds.OfType<Guid>().Distinct().ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await tenantUow.Repository<Customer>().Query()
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
    }
}
