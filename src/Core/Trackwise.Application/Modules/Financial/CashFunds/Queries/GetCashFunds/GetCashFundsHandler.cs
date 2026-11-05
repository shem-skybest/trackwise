using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Queries;

internal sealed class GetCashFundsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetCashFundsQuery, PagedResult<CashFundDto>>
{
    public async Task<PagedResult<CashFundDto>> Handle(GetCashFundsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<CashFund>().Query();

        if (req.Status is { } status)
        {
            query = query.Where(f => f.Status == status);
        }

        if (req.CustodianId is { } custodianId)
        {
            query = query.Where(f => f.CustodianId == custodianId);
        }

        if (req.CoversDate is { } date)
        {
            query = query.Where(f => f.PeriodStart <= date && f.PeriodEnd >= date);
        }

        var totalItems = await query.CountAsync(ct);

        var funds = await (string.IsNullOrWhiteSpace(req.OrderBy)
                ? query.OrderByDescending(f => f.PeriodStart).ThenByDescending(f => f.CreatedAt)
                : query.OrderBy(req.OrderBy))
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var ids = funds.Select(f => f.Id).ToList();
        var tallies = (await tenantUow.Repository<CashFundExpense>().Query()
                .Where(e => ids.Contains(e.CashFundId))
                .GroupBy(e => new { e.CashFundId, e.Status })
                .Select(g => new { g.Key.CashFundId, g.Key.Status, Count = g.Count(), Amount = g.Sum(e => e.Amount) })
                .ToListAsync(ct))
            .ToLookup(t => t.CashFundId, t => new CashExpenseTally(t.Status, t.Count, t.Amount));

        var custodianIds = funds.Select(f => f.CustodianId).Distinct().ToList();
        var names = await tenantUow.Repository<Employee>().Query()
            .IgnoreQueryFilters()
            .Where(e => custodianIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

        var dtos = funds
            .Select(f => f.ToDto(names.GetValueOrDefault(f.CustodianId), tallies[f.Id].ToList()))
            .ToArray();

        return PagedResult<CashFundDto>.Ok(dtos, totalItems, req.PageSize);
    }
}
