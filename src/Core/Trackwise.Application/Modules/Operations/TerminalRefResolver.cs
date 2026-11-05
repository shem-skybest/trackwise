using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations;

/// <summary>
/// Batches the terminal name and code a page of records displays, in one query.
/// <para>
/// Two separate reasons this cannot be a navigation property. Lazy loading is on, so reading one
/// per row is an N+1 the mapper hides; and terminals are soft-deletable, so the global query filter
/// would return null for a port that has since been retired - blanking the origin on a load that
/// genuinely ran through it. Display lookups therefore read past the filter: what a record did
/// reference is a fact about history, not a record a steward is being offered.
/// </para>
/// </summary>
internal static class TerminalRefResolver
{
    public static async Task<IReadOnlyDictionary<Guid, TerminalRef>> ResolveAsync(
        ITenantUnitOfWork uow,
        IReadOnlyCollection<Guid> terminalIds,
        CancellationToken ct = default)
    {
        if (terminalIds.Count == 0)
        {
            return new Dictionary<Guid, TerminalRef>();
        }

        return await uow.Repository<Terminal>().Query()
            .IgnoreQueryFilters()
            .Where(t => terminalIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => new TerminalRef(t.Name, t.Code), ct);
    }
}
