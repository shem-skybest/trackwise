using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Queries;

internal sealed class GetDieselPricesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetDieselPricesQuery, PagedResult<DieselPriceDto>>
{
    public async Task<PagedResult<DieselPriceDto>> Handle(GetDieselPricesQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<DieselPrice>().Query();
        var totalItems = await query.CountAsync(ct);

        var prices = await query
            .OrderByDescending(d => d.EffectiveFrom)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        return PagedResult<DieselPriceDto>.Ok(prices.Select(d => d.ToDto()).ToArray(), totalItems, req.PageSize);
    }
}
