using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetCommoditiesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetCommoditiesQuery, PagedResult<CommodityDto>>
{
    public async Task<PagedResult<CommodityDto>> Handle(
        GetCommoditiesQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Commodity>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Code is stored canonical and the HS code digits-only, so both terms are normalised;
            // the name keeps the raw term. Searching the HS code matters because a steward looking
            // for "8471" wants every commodity under that heading, not one record.
            var code = Commodity.NormalizeCode(req.Search);
            var hs = Commodity.NormalizeDigits(req.Search);
            // Lowercased on both sides: Postgres LIKE is case-sensitive.
            var search = req.Search.Trim().ToLower();

            query = query.Where(c =>
                c.Code.Contains(code)
                || c.Name.ToLower().Contains(search)
                || (hs != null && c.HsCode != null && c.HsCode.StartsWith(hs)));
        }

        if (req.IsHazardous.HasValue)
        {
            query = query.Where(c => c.IsHazardous == req.IsHazardous.Value);
        }

        if (req.RequiresTemperatureControl.HasValue)
        {
            query = query.Where(c =>
                c.RequiresTemperatureControl == req.RequiresTemperatureControl.Value);
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == req.IsActive.Value);
        }

        if (req.BranchId is { } branchId)
        {
            // A null branch means "applies everywhere", so narrowing to one branch has to keep the
            // global records - excluding them would hide the shared commodities the branch uses.
            query = query.Where(c => c.BranchId == branchId || c.BranchId == null);
        }

        if (req.UpdatedSince.HasValue)
        {
            var since = req.UpdatedSince.Value;

            // A record created but never edited has a null UpdatedAt, so falling back to CreatedAt
            // is what stops a delta feed silently skipping every untouched new record.
            query = query.Where(c => (c.UpdatedAt ?? c.CreatedAt) >= since);
        }

        var totalItems = await query.CountAsync(ct);

        var commodities = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var branchNames = await BranchNameLookup.ResolveAsync(
            tenantUow, commodities.Select(c => c.BranchId), ct);

        return PagedResult<CommodityDto>.Ok(
            commodities.Select(c => c.ToDto(branchNames)).ToArray(), totalItems, req.PageSize);
    }
}
