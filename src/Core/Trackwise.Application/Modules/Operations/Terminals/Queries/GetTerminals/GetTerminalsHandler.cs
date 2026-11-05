using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Terminals.Queries;

internal sealed class GetTerminalsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTerminalsQuery, PagedResult<TerminalDto>>
{
    public async Task<PagedResult<TerminalDto>> Handle(GetTerminalsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Terminal>().Query();

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Code is stored canonical; Name is free-form, matched lowercased because Postgres LIKE is case-sensitive.
            var code = Terminal.NormalizeCode(req.Search);
            var search = req.Search.Trim().ToLower();

            query = query.Where(i => i.Name.ToLower().Contains(search) || i.Code.Contains(code));
        }

        if (req.Type.HasValue)
        {
            query = query.Where(i => i.Type == req.Type.Value);
        }

        if (!string.IsNullOrEmpty(req.CountryCode))
        {
            query = query.Where(i => i.CountryCode == req.CountryCode);
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(i => i.IsActive == req.IsActive.Value);
        }

        var totalItems = await query.CountAsync(ct);

        // Soft-deleted terminals are hidden by the global query filter, which is what this route
        // wants: it feeds dispatch pickers, and a retired port must not be offered for new work.
        // The steward's view of deleted records is /master-data/ports with includeDeleted.
        var terminals = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        return PagedResult<TerminalDto>.Ok(
            terminals.Select(i => i.ToDto()).ToArray(), totalItems, req.PageSize);
    }
}
