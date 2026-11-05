using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.FuelLog.Queries;

internal sealed class GetFuelFillUpsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetFuelFillUpsQuery, PagedResult<FuelFillUpDto>>
{
    public async Task<PagedResult<FuelFillUpDto>> Handle(GetFuelFillUpsQuery req, CancellationToken ct)
    {
        var settings = tenantUow.GetCurrentTenant().Settings;
        var query = FuelLogReader.FillUps(tenantUow).Apply(req, settings);

        var totalItems = await query.CountAsync(ct);

        var fills = await (string.IsNullOrWhiteSpace(req.OrderBy)
                ? query.OrderByDescending(e => e.ExpenseDate).ThenByDescending(e => e.Number)
                : query.OrderBy(req.OrderBy))
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var dtos = await FuelLogReader.ToDtosAsync(tenantUow, fills, ct);
        return PagedResult<FuelFillUpDto>.Ok(dtos.ToArray(), totalItems, req.PageSize);
    }
}
