using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Common;

/// <summary>
/// Resolves the container-type code and name for a page of records that reference one, in one
/// query.
/// <para>
/// Shared rather than copied per caller, and the reason this exists at all is the conversion that
/// created it: an enum rendered itself, a foreign key does not. Lazy loading is on, so reading
/// <c>record.ContainerType</c> inside a projection loop is a query per row - containers, bookings
/// and tariffs would each have been a copy of the identical batch. <c>BranchNameLookup</c> is the
/// same shape for the same reason.
/// </para>
/// </summary>
public static class ContainerTypeLookup
{
    public static async Task<IReadOnlyDictionary<Guid, ContainerTypeRef>> ResolveAsync(
        ITenantUnitOfWork tenantUow, IEnumerable<Guid?> containerTypeIds, CancellationToken ct)
    {
        var ids = containerTypeIds.OfType<Guid>().Distinct().ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, ContainerTypeRef>();
        }

        // Past the filter: a booking placed for a type a steward later deleted must still name the
        // box it was for, rather than rendering as though no equipment was ever chosen.
        return await tenantUow.Repository<ContainerType>().Query()
            .IgnoreQueryFilters()
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new ContainerTypeRef(c.Code, c.Name), ct);
    }
}
