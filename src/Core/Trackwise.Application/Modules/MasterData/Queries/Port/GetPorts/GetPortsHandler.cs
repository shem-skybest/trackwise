using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetPortsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetPortsQuery, PagedResult<TerminalDto>>
{
    public async Task<PagedResult<TerminalDto>> Handle(GetPortsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Terminal>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Code is stored canonical, so the term must be normalised or "uslax" matches nothing.
            // Name is free-form, matched lowercased because Postgres LIKE is case-sensitive.
            var code = Terminal.NormalizeCode(req.Search);
            var search = req.Search.Trim().ToLower();

            query = query.Where(t => t.Code.Contains(code) || t.Name.ToLower().Contains(search));
        }

        if (req.Type.HasValue)
        {
            query = query.Where(t => t.Type == req.Type.Value);
        }

        if (!string.IsNullOrEmpty(req.CountryCode))
        {
            query = query.Where(t => t.CountryCode == req.CountryCode);
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == req.IsActive.Value);
        }

        if (req.BranchId is { } branchId)
        {
            // A null branch means "applies everywhere", so narrowing to one branch has to keep the
            // global records - excluding them would hide the shared ports the branch actually uses.
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

        var terminals = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var branchNames = await BranchNameLookup.ResolveAsync(
            tenantUow, terminals.Select(t => t.BranchId), ct);

        return PagedResult<TerminalDto>.Ok(
            terminals.Select(t => t.ToDto(branchNames)).ToArray(), totalItems, req.PageSize);
    }
}
