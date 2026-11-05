using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestoreCurrencyHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestoreCurrencyCommand, Result>
{
    public async Task<Result> Handle(RestoreCurrencyCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var currency = await tenantUow.Repository<Currency>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        if (currency is null)
        {
            return Result.Fail($"Could not find currency with ID '{req.Id}'");
        }

        if (currency.DeletedAt is null)
        {
            return Result.Fail("That currency is not deleted.");
        }

        // Codes are unique among live rows only, so restoring can collide with a code that was
        // taken while this record was out of sight.
        var code = currency.Code;
        var holder = await tenantUow.Repository<Currency>().Query()
            .FirstOrDefaultAsync(c => c.Code == code && c.Id != currency.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the code first.");
        }

        var numericHolder = await CurrencyNumericLookup.FindHolderAsync(
            tenantUow, currency.NumericCode, currency.Id, ct);

        if (numericHolder is not null && numericHolder.DeletedAt is null)
        {
            return Result.Fail(
                $"Numeric code '{currency.NumericCode}' is now used by {numericHolder.Code} "
                + $"({numericHolder.Name}). Merge this record into that one instead.");
        }

        currency.DeletedAt = null;
        currency.DeletedBy = null;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
