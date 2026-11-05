using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Queries;

internal sealed class GetRateAgreementsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetRateAgreementsQuery, PagedResult<RateAgreementDto>>
{
    public async Task<PagedResult<RateAgreementDto>> Handle(GetRateAgreementsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<RateAgreement>().Query();

        if (req.CustomerId is { } customerId)
        {
            query = query.Where(a => a.CustomerId == customerId);
        }

        if (req.Status is { } status)
        {
            query = query.Where(a => a.Status == status);
        }

        if (req.InForceOn is { } date)
        {
            query = query.Where(a =>
                (a.Status == RateAgreementStatus.Active || a.Status == RateAgreementStatus.Superseded)
                && a.EffectiveFrom <= date
                && (a.EffectiveTo == null || a.EffectiveTo >= date));
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.Trim().ToLower();
            query = query.Where(a => a.Name.ToLower().Contains(search));
        }

        var totalItems = await query.CountAsync(ct);

        var agreements = await (string.IsNullOrWhiteSpace(req.OrderBy)
                ? query.OrderBy(a => a.CustomerId).ThenByDescending(a => a.EffectiveFrom)
                : query.OrderBy(req.OrderBy))
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var ids = agreements.Select(a => a.Id).ToList();
        var lineCounts = await tenantUow.Repository<RateAgreementLine>().Query()
            .Where(l => ids.Contains(l.RateAgreementId))
            .GroupBy(l => l.RateAgreementId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

        var customerNames = await CustomerNameLookup.ResolveAsync(
            tenantUow, agreements.Select(a => (Guid?)a.CustomerId), ct);

        var dtos = agreements
            .Select(a => a.ToHeaderDto() with
            {
                CustomerName = customerNames.GetValueOrDefault(a.CustomerId),
                LineCount = lineCounts.GetValueOrDefault(a.Id)
            })
            .ToArray();

        return PagedResult<RateAgreementDto>.Ok(dtos, totalItems, req.PageSize);
    }
}
