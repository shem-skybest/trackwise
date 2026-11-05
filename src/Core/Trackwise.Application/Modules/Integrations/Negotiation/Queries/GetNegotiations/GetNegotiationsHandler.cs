using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Integrations.Negotiation.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Integrations.Negotiation.Queries;

internal sealed class GetNegotiationsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetNegotiationsQuery, PagedResult<RateNegotiationDto>>
{
    public async Task<PagedResult<RateNegotiationDto>> Handle(GetNegotiationsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<RateNegotiation>().Query();

        if (req.Status.HasValue)
        {
            query = query.Where(n => n.Status == req.Status.Value);
        }
        else if (req.ActiveOnly)
        {
            query = query.Where(RateNegotiation.Open());
        }

        var totalItems = await query.CountAsync(ct);

        var negotiations = await query
            .OrderBy(req.OrderBy ?? "-CreatedAt")
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var dtos = await NegotiationDtoBatch.MapAsync(tenantUow, negotiations, ct);

        return PagedResult<RateNegotiationDto>.Ok(dtos, totalItems, req.PageSize);
    }
}
