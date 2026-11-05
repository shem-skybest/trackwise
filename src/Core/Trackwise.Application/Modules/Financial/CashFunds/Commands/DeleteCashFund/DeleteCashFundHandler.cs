using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

internal sealed class DeleteCashFundHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteCashFundCommand, Result>
{
    public async Task<Result> Handle(DeleteCashFundCommand req, CancellationToken ct)
    {
        var fund = await tenantUow.Repository<CashFund>().GetByIdAsync(req.Id, ct);
        if (fund is null)
        {
            return Result.Fail($"Could not find cash fund with ID '{req.Id}'.");
        }

        if (fund.Status != CashFundStatus.Open || fund.Expenses.Any(e => e.Status != CashExpenseStatus.WithOps))
        {
            return Result.Fail("Receipts from this fund already reached the office, so it can't be deleted.");
        }

        tenantUow.Repository<CashFund>().Delete(fund);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
