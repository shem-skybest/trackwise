using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

internal sealed class UpdateCashFundHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateCashFundCommand, Result<CashFundDto>>
{
    public async Task<Result<CashFundDto>> Handle(UpdateCashFundCommand req, CancellationToken ct)
    {
        var fund = await tenantUow.Repository<CashFund>().GetByIdAsync(req.Id, ct);
        if (fund is null)
        {
            return Result<CashFundDto>.Fail($"Could not find cash fund with ID '{req.Id}'.");
        }

        if (await tenantUow.Repository<Employee>().GetByIdAsync(req.CustodianId, ct) is null)
        {
            return Result<CashFundDto>.Fail($"Could not find the employee '{req.CustodianId}'.");
        }

        try
        {
            fund.UpdateTerms(req.CustodianId, req.PeriodStart, req.PeriodEnd, req.Amount, req.AdvanceReference);
            fund.Notes = req.Notes;
            await tenantUow.SaveChangesAsync(ct);
            return Result<CashFundDto>.Ok(await CashFundReader.ToDtoAsync(tenantUow, fund, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<CashFundDto>.Fail(ex.Message);
        }
    }
}
