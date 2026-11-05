using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetCurrenciesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetCurrenciesQuery, PagedResult<CurrencyDto>>
{
    public async Task<PagedResult<CurrencyDto>> Handle(
        GetCurrenciesQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Currency>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Code is stored canonical and the numeric code digits-only, so both terms are
            // normalised; the name is matched lowercased because Postgres LIKE is case-sensitive.
            var code = Currency.NormalizeCode(req.Search);
            var numeric = Currency.NormalizeDigits(req.Search);
            var search = req.Search.Trim().ToLower();

            query = query.Where(c =>
                c.Code.Contains(code)
                || c.Name.ToLower().Contains(search)
                || (numeric != null && c.NumericCode != null && c.NumericCode.StartsWith(numeric)));
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == req.IsActive.Value);
        }

        if (req.UpdatedSince.HasValue)
        {
            var since = req.UpdatedSince.Value;

            // A record created but never edited has a null UpdatedAt, so falling back to CreatedAt
            // is what stops a delta feed silently skipping every untouched new record.
            query = query.Where(c => (c.UpdatedAt ?? c.CreatedAt) >= since);
        }

        var totalItems = await query.CountAsync(ct);

        var currencies = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        return PagedResult<CurrencyDto>.Ok(
            currencies.Select(c => c.ToDto()).ToArray(), totalItems, req.PageSize);
    }
}
