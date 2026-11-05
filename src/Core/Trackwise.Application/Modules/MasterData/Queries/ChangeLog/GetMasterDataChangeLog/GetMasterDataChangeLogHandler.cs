using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetMasterDataChangeLogHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetMasterDataChangeLogQuery, PagedResult<MasterDataChangeLogDto>>
{
    public async Task<PagedResult<MasterDataChangeLogDto>> Handle(
        GetMasterDataChangeLogQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<MasterDataChangeLog>().Query()
            .Where(l => l.EntityType == req.EntityType && l.EntityId == req.EntityId);

        var totalItems = await query.CountAsync(ct);

        // Fixed ordering rather than req.OrderBy: a history read backwards is not a history, and
        // the covering index is built for this one shape.
        var entries = await query
            .OrderByDescending(l => l.ChangedAt)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        return PagedResult<MasterDataChangeLogDto>.Ok(
            entries.Select(l => l.ToDto()).ToArray(), totalItems, req.PageSize);
    }
}
