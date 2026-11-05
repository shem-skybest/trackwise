using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Common;

/// <summary>
/// Finds the record already holding an ISO 6346 size-type code, so create and edit refuse it with
/// the same message instead of meeting the filtered unique index as a constraint violation.
/// </summary>
internal static class ContainerTypeIsoLookup
{
    /// <summary>
    /// The record holding <paramref name="iso"/>, or null when it is free. Reads past the query
    /// filter because the index is filtered the same way: a soft-deleted type still owns its ISO
    /// code, and a steward reusing it should be told which record they are colliding with.
    /// </summary>
    public static async Task<ContainerType?> FindHolderAsync(
        ITenantUnitOfWork tenantUow, string? iso, Guid? excluding, CancellationToken ct)
    {
        if (iso is null)
        {
            return null;
        }

        var query = tenantUow.Repository<ContainerType>().Query()
            .IgnoreQueryFilters()
            .Where(c => c.IsoSizeTypeCode == iso);

        // Written as a separate clause rather than "c.Id != excluding": comparing against a null
        // Guid? in SQL yields NULL, not true, so the inline form would silently match nothing on
        // create - exactly the path this check exists for.
        if (excluding is { } id)
        {
            query = query.Where(c => c.Id != id);
        }

        return await query.FirstOrDefaultAsync(ct);
    }
}
