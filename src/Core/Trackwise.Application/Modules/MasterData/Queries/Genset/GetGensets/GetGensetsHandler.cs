using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetGensetsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetGensetsQuery, PagedResult<GensetDto>>
{
    public async Task<PagedResult<GensetDto>> Handle(GetGensetsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Genset>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            var code = Genset.NormalizeCode(req.Search);
            var serialNumber = Genset.NormalizeSerialNumber(req.Search);
            // Lowercased on both sides: Postgres LIKE is case-sensitive.
            var search = req.Search.Trim().ToLower();

            query = query.Where(g =>
                g.Code.Contains(code)
                || g.Name.ToLower().Contains(search)
                || (serialNumber != null && g.SerialNumber != null && g.SerialNumber.Contains(serialNumber)));
        }

        if (req.Status.HasValue)
        {
            query = query.Where(g => g.Status == req.Status.Value);
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(g => g.IsActive == req.IsActive.Value);
        }

        if (req.BranchId is { } branchId)
        {
            // A null branch means "applies everywhere", so narrowing to one branch has to keep the
            // global records - excluding them would hide the shared gensets the branch actually uses.
            query = query.Where(g => g.BranchId == branchId || g.BranchId == null);
        }

        if (req.UpdatedSince.HasValue)
        {
            var since = req.UpdatedSince.Value;

            // A record created but never edited has a null UpdatedAt, so falling back to CreatedAt
            // is what stops a delta feed silently skipping every untouched new record.
            query = query.Where(g => (g.UpdatedAt ?? g.CreatedAt) >= since);
        }

        var totalItems = await query.CountAsync(ct);

        var gensets = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var branchNames = await BranchNameLookup.ResolveAsync(
            tenantUow, gensets.Select(g => g.BranchId), ct);

        return PagedResult<GensetDto>.Ok(
            gensets.Select(g => g.ToDto(branchNames)).ToArray(), totalItems, req.PageSize);
    }
}
