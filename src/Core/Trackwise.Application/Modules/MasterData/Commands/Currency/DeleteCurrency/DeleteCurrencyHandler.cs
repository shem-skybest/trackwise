using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteCurrencyHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteCurrencyCommand, Result>
{
    public async Task<Result> Handle(DeleteCurrencyCommand req, CancellationToken ct)
    {
        var currency = await tenantUow.Repository<Currency>().GetByIdAsync(req.Id, ct);
        if (currency is null)
        {
            return Result.Fail($"Could not find currency with ID '{req.Id}'");
        }

        // The dependency probe runs in the pipeline off this entity's declared rules, and Currency
        // declares none - not because nothing uses a currency, but because every Money in the
        // schema holds its code as text and there is no foreign-key column for the probe to count.
        // Deleting one is genuinely unguarded; the policy table says so at length.
        tenantUow.Repository<Currency>().Delete(currency);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
