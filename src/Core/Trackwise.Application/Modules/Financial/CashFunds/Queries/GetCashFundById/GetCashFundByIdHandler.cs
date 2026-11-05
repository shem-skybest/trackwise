using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds.Queries;

internal sealed class GetCashFundByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetCashFundByIdQuery, Result<CashFundDto>>
{
    public async Task<Result<CashFundDto>> Handle(GetCashFundByIdQuery req, CancellationToken ct)
    {
        var fund = await tenantUow.Repository<CashFund>().GetByIdAsync(req.Id, ct);
        return fund is null
            ? Result<CashFundDto>.Fail($"Could not find cash fund with ID '{req.Id}'.")
            : Result<CashFundDto>.Ok(await CashFundReader.ToDtoAsync(tenantUow, fund, ct));
    }
}
