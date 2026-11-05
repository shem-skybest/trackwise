using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetBranchesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetBranchesQuery, PagedResult<BranchDto>>
{
    public async Task<PagedResult<BranchDto>> Handle(GetBranchesQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Branch>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Code is stored canonical, so the term must be normalised or "mnl" matches nothing.
            // Name and short name are free-form, matched lowercased because Postgres LIKE is case-sensitive.
            var code = Branch.NormalizeCode(req.Search);
            var search = req.Search.Trim().ToLower();

            query = query.Where(b => b.Code.Contains(code)
                                     || b.Name.ToLower().Contains(search)
                                     || (b.ShortName != null && b.ShortName.ToLower().Contains(search)));
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(b => b.IsActive == req.IsActive.Value);
        }

        if (req.UpdatedSince.HasValue)
        {
            var since = req.UpdatedSince.Value;

            // A record created but never edited has a null UpdatedAt, so falling back to CreatedAt
            // is what stops a delta feed silently skipping every untouched new record.
            query = query.Where(b => (b.UpdatedAt ?? b.CreatedAt) >= since);
        }

        var totalItems = await query.CountAsync(ct);

        var branches = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        return PagedResult<BranchDto>.Ok(
            branches.Select(b => b.ToDto()).ToArray(), totalItems, req.PageSize);
    }
}
