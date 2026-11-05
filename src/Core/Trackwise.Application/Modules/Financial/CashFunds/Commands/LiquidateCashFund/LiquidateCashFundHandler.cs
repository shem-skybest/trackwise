using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

internal sealed class LiquidateCashFundHandler(ITenantUnitOfWork tenantUow, ICurrentUserService currentUser)
    : IAppRequestHandler<LiquidateCashFundCommand, Result<CashFundDto>>
{
    public async Task<Result<CashFundDto>> Handle(LiquidateCashFundCommand req, CancellationToken ct)
    {
        var fund = await tenantUow.Repository<CashFund>().GetByIdAsync(req.Id, ct);
        if (fund is null)
        {
            return Result<CashFundDto>.Fail($"Could not find cash fund with ID '{req.Id}'.");
        }

        try
        {
            fund.Liquidate(currentUser.GetUserName(), DateTime.UtcNow);
            await tenantUow.SaveChangesAsync(ct);
            return Result<CashFundDto>.Ok(await CashFundReader.ToDtoAsync(tenantUow, fund, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<CashFundDto>.Fail(ex.Message);
        }
    }
}
