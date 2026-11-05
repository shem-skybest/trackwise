using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetContainerTypesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetContainerTypesQuery, PagedResult<ContainerTypeDto>>
{
    public async Task<PagedResult<ContainerTypeDto>> Handle(
        GetContainerTypesQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<ContainerType>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Both codes are stored canonical, so both terms are normalised; the name keeps the raw
            // term. Searching the ISO code matters because half the industry knows a 40' high cube
            // only as 45G1.
            var code = ContainerType.NormalizeCode(req.Search);
            var iso = ContainerType.NormalizeIsoSizeTypeCode(req.Search);
            // Lowercased on both sides: Postgres LIKE is case-sensitive.
            var search = req.Search.Trim().ToLower();

            query = query.Where(c =>
                c.Code.Contains(code)
                || c.Name.ToLower().Contains(search)
                || (iso != null && c.IsoSizeTypeCode != null && c.IsoSizeTypeCode.Contains(iso)));
        }

        if (req.Category.HasValue)
        {
            query = query.Where(c => c.Category == req.Category.Value);
        }

        if (req.LengthFeet.HasValue)
        {
            query = query.Where(c => c.LengthFeet == req.LengthFeet.Value);
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == req.IsActive.Value);
        }

        if (req.UpdatedSince.HasValue)
        {
            var since = req.UpdatedSince.Value;

            // A record created but never edited has a null UpdatedAt, so falling back to CreatedAt
            // is what stops a delta feed silently skipping every untouched new record.
            query = query.Where(c => (c.UpdatedAt ?? c.CreatedAt) >= since);
        }

        var totalItems = await query.CountAsync(ct);

        var containerTypes = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        return PagedResult<ContainerTypeDto>.Ok(
            containerTypes.Select(c => c.ToDto()).ToArray(), totalItems, req.PageSize);
    }
}
