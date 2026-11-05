using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Common;

/// <summary>
/// Finds the record already holding an ISO 4217 numeric code, so create and edit refuse it with the
/// same message instead of meeting the filtered unique index as a constraint violation.
/// </summary>
internal static class CurrencyNumericLookup
{
    /// <summary>
    /// The record holding <paramref name="numericCode"/>, or null when it is free. Reads past the
    /// query filter because the index is filtered the same way: a soft-deleted currency still owns
    /// its numeric code.
    /// </summary>
    public static async Task<Currency?> FindHolderAsync(
        ITenantUnitOfWork tenantUow, string? numericCode, Guid? excluding, CancellationToken ct)
    {
        if (numericCode is null)
        {
            return null;
        }

        var query = tenantUow.Repository<Currency>().Query()
            .IgnoreQueryFilters()
            .Where(c => c.NumericCode == numericCode);

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
