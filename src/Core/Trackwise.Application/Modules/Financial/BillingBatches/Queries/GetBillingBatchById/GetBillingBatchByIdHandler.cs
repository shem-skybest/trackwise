using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Queries;

internal sealed class GetBillingBatchByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetBillingBatchByIdQuery, Result<BillingBatchDto>>
{
    public async Task<Result<BillingBatchDto>> Handle(GetBillingBatchByIdQuery req, CancellationToken ct)
    {
        var batch = await tenantUow.Repository<BillingBatch>().GetByIdAsync(req.Id, ct);
        return batch is null
            ? Result<BillingBatchDto>.Fail($"Could not find billing batch with ID '{req.Id}'.")
            : Result<BillingBatchDto>.Ok(await BillingBatchReader.ToDtoAsync(tenantUow, batch, ct));
    }
}
