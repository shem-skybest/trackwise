using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetVesselsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetVesselsQuery, PagedResult<VesselDto>>
{
    public async Task<PagedResult<VesselDto>> Handle(GetVesselsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Vessel>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Three stored forms, three shapes of search term. Code is canonical upper-case, the
            // IMO column holds digits only, and the name keeps whatever the steward typed - so a
            // steward pasting "IMO 9395044" finds the ship and typing "ever" finds it too.
            var code = Vessel.NormalizeCode(req.Search);
            var imo = Vessel.NormalizeImoNumber(req.Search);
            // Lowercased on both sides: Postgres LIKE is case-sensitive.
            var search = req.Search.Trim().ToLower();

            query = query.Where(v =>
                v.Code.Contains(code)
                || v.Name.ToLower().Contains(search)
                || (imo != null && v.ImoNumber != null && v.ImoNumber.Contains(imo)));
        }

        if (req.Type.HasValue)
        {
            query = query.Where(v => v.Type == req.Type.Value);
        }

        if (!string.IsNullOrEmpty(req.FlagCountryCode))
        {
            query = query.Where(v => v.FlagCountryCode == req.FlagCountryCode);
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(v => v.IsActive == req.IsActive.Value);
        }

        if (req.BranchId is { } branchId)
        {
            // A null branch means "applies everywhere", so narrowing to one branch has to keep the
            // global records - excluding them would hide the shared vessels the branch actually uses.
            query = query.Where(v => v.BranchId == branchId || v.BranchId == null);
        }

        if (req.UpdatedSince.HasValue)
        {
            var since = req.UpdatedSince.Value;

            // A record created but never edited has a null UpdatedAt, so falling back to CreatedAt
            // is what stops a delta feed silently skipping every untouched new record.
            query = query.Where(v => (v.UpdatedAt ?? v.CreatedAt) >= since);
        }

        var totalItems = await query.CountAsync(ct);

        var vessels = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var branchNames = await BranchNameLookup.ResolveAsync(
            tenantUow, vessels.Select(v => v.BranchId), ct);

        return PagedResult<VesselDto>.Ok(
            vessels.Select(v => v.ToDto(branchNames)).ToArray(), totalItems, req.PageSize);
    }
}
