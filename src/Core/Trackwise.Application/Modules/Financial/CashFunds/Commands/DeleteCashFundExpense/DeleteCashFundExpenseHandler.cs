using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

internal sealed class DeleteCashFundExpenseHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteCashFundExpenseCommand, Result<CashFundDto>>
{
    public async Task<Result<CashFundDto>> Handle(DeleteCashFundExpenseCommand req, CancellationToken ct)
    {
        var fund = await tenantUow.Repository<CashFund>().GetByIdAsync(req.CashFundId, ct);
        if (fund is null)
        {
            return Result<CashFundDto>.Fail($"Could not find cash fund with ID '{req.CashFundId}'.");
        }

        try
        {
            tenantUow.Repository<CashFundExpense>().Delete(fund.RemoveExpense(req.ExpenseId));
            await tenantUow.SaveChangesAsync(ct);
            return Result<CashFundDto>.Ok(await CashFundReader.ToDtoAsync(tenantUow, fund, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<CashFundDto>.Fail(ex.Message);
        }
    }
}
