using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetTrailersHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTrailersQuery, PagedResult<TrailerDto>>
{
    public async Task<PagedResult<TrailerDto>> Handle(GetTrailersQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Trailer>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            var code = Trailer.NormalizeCode(req.Search);
            var vin = Trailer.NormalizeVin(req.Search);
            // Lowercased on both sides: Postgres LIKE is case-sensitive.
            var search = req.Search.Trim().ToLower();

            query = query.Where(t =>
                t.Code.Contains(code)
                || t.Name.ToLower().Contains(search)
                || (vin != null && t.Vin != null && t.Vin.Contains(vin)));
        }

        if (req.Type.HasValue)
        {
            query = query.Where(t => t.Type == req.Type.Value);
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == req.IsActive.Value);
        }

        if (req.BranchId is { } branchId)
        {
            // A null branch means "applies everywhere", so narrowing to one branch has to keep the
            // global records - excluding them would hide the shared trailers the branch actually uses.
            query = query.Where(t => t.BranchId == branchId || t.BranchId == null);
        }

        if (req.UpdatedSince.HasValue)
        {
            var since = req.UpdatedSince.Value;

            // A record created but never edited has a null UpdatedAt, so falling back to CreatedAt
            // is what stops a delta feed silently skipping every untouched new record.
            query = query.Where(t => (t.UpdatedAt ?? t.CreatedAt) >= since);
        }

        var totalItems = await query.CountAsync(ct);

        var trailers = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var branchNames = await BranchNameLookup.ResolveAsync(
            tenantUow, trailers.Select(t => t.BranchId), ct);

        return PagedResult<TrailerDto>.Ok(
            trailers.Select(t => t.ToDto(branchNames)).ToArray(), totalItems, req.PageSize);
    }
}
