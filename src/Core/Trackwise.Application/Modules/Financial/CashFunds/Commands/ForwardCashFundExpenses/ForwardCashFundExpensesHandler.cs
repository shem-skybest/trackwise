using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

internal sealed class ForwardCashFundExpensesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<ForwardCashFundExpensesCommand, Result<CashFundDto>>
{
    public async Task<Result<CashFundDto>> Handle(ForwardCashFundExpensesCommand req, CancellationToken ct)
    {
        var fund = await tenantUow.Repository<CashFund>().GetByIdAsync(req.Id, ct);
        if (fund is null)
        {
            return Result<CashFundDto>.Fail($"Could not find cash fund with ID '{req.Id}'.");
        }

        try
        {
            var today = tenantUow.GetCurrentTenant().Settings.LocalDate(DateTime.UtcNow);
            fund.Forward(req.ExpenseIds.Distinct().ToList(), today);
            await tenantUow.SaveChangesAsync(ct);
            return Result<CashFundDto>.Ok(await CashFundReader.ToDtoAsync(tenantUow, fund, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<CashFundDto>.Fail(ex.Message);
        }
    }
}
