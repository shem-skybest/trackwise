using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.RemoveTariffRate;

internal sealed class RemoveTariffRateHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RemoveTariffRateCommand, Result>
{
    public async Task<Result> Handle(RemoveTariffRateCommand req, CancellationToken ct)
    {
        var tariffRateRepo = tenantUow.Repository<TariffRate>();

        // 1. Validate entity existence first (fail-fast)
        var tariffRate = await tariffRateRepo
            .Query()
            .FirstOrDefaultAsync(tr => tr.Id == req.RateId, ct);

        if (tariffRate is null)
            return Result.Fail($"TariffRate with ID '{req.RateId}' was not found.");

        // 2. Check relational dependencies (with cancellation token passed)
        var isUsedInQuotationLines = await tenantUow.Repository<QuotationLine>()
            .Query()
            .AnyAsync(ql => ql.TariffRateId == req.RateId, ct);

        if (isUsedInQuotationLines)
            return Result.Fail("Cannot delete this tariff rate because it is actively referenced by one or more quotation lines.");

        // 3. Delete and commit
        tariffRateRepo.Delete(tariffRate);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
