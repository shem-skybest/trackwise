using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

internal sealed class UpdateCashFundExpenseHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateCashFundExpenseCommand, Result<CashFundDto>>
{
    public async Task<Result<CashFundDto>> Handle(UpdateCashFundExpenseCommand req, CancellationToken ct)
    {
        var fund = await tenantUow.Repository<CashFund>().GetByIdAsync(req.CashFundId, ct);
        if (fund is null)
        {
            return Result<CashFundDto>.Fail($"Could not find cash fund with ID '{req.CashFundId}'.");
        }

        var spec = await CashExpenseReferences.ResolveAsync(tenantUow, req.ToSpec(), ct);
        if (!spec.IsSuccess)
        {
            return Result<CashFundDto>.Fail(spec.Error!);
        }

        try
        {
            fund.UpdateExpense(req.ExpenseId, spec.Value!);
            await tenantUow.SaveChangesAsync(ct);
            return Result<CashFundDto>.Ok(await CashFundReader.ToDtoAsync(tenantUow, fund, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<CashFundDto>.Fail(ex.Message);
        }
    }
}
