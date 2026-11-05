using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

internal sealed class SetRateAgreementBillingRulesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<SetRateAgreementBillingRulesCommand, Result>
{
    public async Task<Result> Handle(SetRateAgreementBillingRulesCommand req, CancellationToken ct)
    {
        var agreement = await tenantUow.Repository<RateAgreement>().GetByIdAsync(req.Id, ct);
        if (agreement is null)
        {
            return Result.Fail($"Could not find rate agreement with ID '{req.Id}'");
        }

        try
        {
            agreement.SetBillingRules(req.BillExtraDrops, req.RatesIncludeVat);
            await tenantUow.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}
