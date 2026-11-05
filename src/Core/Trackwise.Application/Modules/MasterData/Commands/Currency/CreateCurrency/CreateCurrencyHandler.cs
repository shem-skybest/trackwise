using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateCurrencyHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CreateCurrencyCommand, Result<CurrencyDto>>
{
    public async Task<Result<CurrencyDto>> Handle(CreateCurrencyCommand req, CancellationToken ct)
    {
        var code = Currency.NormalizeCode(req.Code);

        // No number-series branch here, and that is the difference between this list and every
        // other one: a currency's code is ISO 4217's to assign, so there is nothing to allocate.
        // Read past the query filter - the unique index is filtered to live rows, so a clash with a
        // deleted currency is allowed, but the steward should be told what they are reusing.
        var clash = await tenantUow.Repository<Currency>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Code == code, ct);

        if (clash is not null)
        {
            return Result<CurrencyDto>.Fail(clash.DeletedAt is null
                ? $"A currency with code '{code}' already exists"
                : $"Code '{code}' belonged to a deleted currency. Restore that currency instead of creating it again.");
        }

        var numeric = Currency.NormalizeDigits(req.NumericCode);
        var numericHolder = await CurrencyNumericLookup.FindHolderAsync(tenantUow, numeric, null, ct);

        if (numericHolder is not null)
        {
            return Result<CurrencyDto>.Fail(
                $"Numeric code '{numeric}' already belongs to {numericHolder.Code} ({numericHolder.Name}).");
        }

        var currency = new Currency
        {
            Code = code,
            Name = req.Name,
            NumericCode = numeric,
            Symbol = req.Symbol,
            MinorUnits = req.MinorUnits,
            Notes = req.Notes
        };

        await tenantUow.Repository<Currency>().AddAsync(currency, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<CurrencyDto>.Ok(currency.ToDto());
    }
}
