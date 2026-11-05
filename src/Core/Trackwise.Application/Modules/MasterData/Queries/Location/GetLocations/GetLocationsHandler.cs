using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetLocationsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetLocationsQuery, PagedResult<LocationDto>>
{
    public async Task<PagedResult<LocationDto>> Handle(GetLocationsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Location>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Code is stored canonical, so the term must be normalised or "loc-1" matches nothing.
            // Name is free-form, matched lowercased because Postgres LIKE is case-sensitive.
            var code = Location.NormalizeCode(req.Search);
            var search = req.Search.Trim().ToLower();

            query = query.Where(l => l.Code.Contains(code) || l.Name.ToLower().Contains(search));
        }

        if (req.Type.HasValue)
        {
            query = query.Where(l => l.Type == req.Type.Value);
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(l => l.IsActive == req.IsActive.Value);
        }

        if (req.ParentLocationId is { } parentId)
        {
            // Exact, unlike the branch filter below: asking what is inside a zone means that zone,
            // and folding in the unfiled locations would answer a different question.
            query = query.Where(l => l.ParentLocationId == parentId);
        }

        if (req.BranchId is { } branchId)
        {
            // A null branch means "applies everywhere", so narrowing to one branch has to keep the
            // global records - excluding them would hide the shared locations the branch uses.
            query = query.Where(l => l.BranchId == branchId || l.BranchId == null);
        }

        if (req.CustomerId is { } customerId)
        {
            query = req.IncludeShared
                ? query.Where(l => l.CustomerId == customerId || l.CustomerId == null)
                : query.Where(l => l.CustomerId == customerId);
        }

        if (req.UpdatedSince.HasValue)
        {
            var since = req.UpdatedSince.Value;

            // A record created but never edited has a null UpdatedAt, so falling back to CreatedAt
            // is what stops a delta feed silently skipping every untouched new record.
            query = query.Where(l => (l.UpdatedAt ?? l.CreatedAt) >= since);
        }

        var totalItems = await query.CountAsync(ct);

        var locations = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var branchNames = await BranchNameLookup.ResolveAsync(
            tenantUow, locations.Select(l => l.BranchId), ct);

        var parentNames = await ParentNamesAsync(locations, ct);

        var customerNames = await CustomerNameLookup.ResolveAsync(
            tenantUow, locations.Select(l => l.CustomerId), ct);

        return PagedResult<LocationDto>.Ok(
            locations.Select(l => l.ToDto(branchNames, parentNames, customerNames)).ToArray(),
            totalItems,
            req.PageSize);
    }

    /// <summary>
    /// Resolves the zone names for a page in one query. The same batch as the branch names and for
    /// the same reason - reading <c>location.ParentLocation</c> in the projection loop would
    /// lazy-load once per row, and unlike the branch there is one of these for most records.
    /// </summary>
    internal async Task<IReadOnlyDictionary<Guid, string>> ParentNamesAsync(
        IReadOnlyCollection<Location> locations, CancellationToken ct)
    {
        var ids = locations
            .Select(l => l.ParentLocationId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        // Past the filter: a location inside a zone somebody deleted should still name that zone,
        // rather than rendering as though it were unfiled.
        return await tenantUow.Repository<Location>().Query()
            .IgnoreQueryFilters()
            .Where(l => ids.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.Name, ct);
    }
}
