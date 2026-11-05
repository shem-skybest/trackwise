using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateCurrencyHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateCurrencyCommand, Result>
{
    public async Task<Result> Handle(UpdateCurrencyCommand req, CancellationToken ct)
    {
        var currency = await tenantUow.Repository<Currency>().GetByIdAsync(req.Id, ct);
        if (currency is null)
        {
            return Result.Fail($"Could not find currency with ID '{req.Id}'");
        }

        var numeric = Currency.NormalizeDigits(req.NumericCode);
        var numericHolder = await CurrencyNumericLookup.FindHolderAsync(
            tenantUow, numeric, currency.Id, ct);

        if (numericHolder is not null)
        {
            return Result.Fail(
                $"Numeric code '{numeric}' already belongs to {numericHolder.Code} ({numericHolder.Name}).");
        }

        currency.Name = req.Name;
        currency.NumericCode = numeric;
        currency.Symbol = req.Symbol;
        currency.MinorUnits = req.MinorUnits;
        currency.Notes = req.Notes;
        currency.IsActive = req.IsActive;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
