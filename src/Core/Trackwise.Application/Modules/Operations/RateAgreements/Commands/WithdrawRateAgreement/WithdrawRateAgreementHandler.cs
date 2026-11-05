using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

internal sealed class WithdrawRateAgreementHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<WithdrawRateAgreementCommand, Result>
{
    public async Task<Result> Handle(WithdrawRateAgreementCommand req, CancellationToken ct)
    {
        var agreement = await tenantUow.Repository<RateAgreement>().GetByIdAsync(req.Id, ct);
        if (agreement is null)
        {
            return Result.Fail($"Could not find rate agreement with ID '{req.Id}'");
        }

        try
        {
            agreement.Withdraw();
            await tenantUow.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}
