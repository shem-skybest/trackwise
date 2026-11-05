using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

internal sealed class CreateCashFundHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CreateCashFundCommand, Result<CashFundDto>>
{
    public async Task<Result<CashFundDto>> Handle(CreateCashFundCommand req, CancellationToken ct)
    {
        if (await tenantUow.Repository<Employee>().GetByIdAsync(req.CustodianId, ct) is null)
        {
            return Result<CashFundDto>.Fail($"Could not find the employee '{req.CustodianId}'.");
        }

        try
        {
            var fund = CashFund.Create(req.CustodianId, req.PeriodStart, req.PeriodEnd, req.Amount, req.AdvanceReference);
            fund.Notes = req.Notes;

            await tenantUow.Repository<CashFund>().AddAsync(fund, ct);
            await tenantUow.SaveChangesAsync(ct);
            return Result<CashFundDto>.Ok(await CashFundReader.ToDtoAsync(tenantUow, fund, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<CashFundDto>.Fail(ex.Message);
        }
    }
}
