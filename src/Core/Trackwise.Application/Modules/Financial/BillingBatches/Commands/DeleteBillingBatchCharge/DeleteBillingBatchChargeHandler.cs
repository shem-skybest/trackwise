using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class DeleteBillingBatchChargeHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteBillingBatchChargeCommand, Result<BillingBatchDto>>
{
    public Task<Result<BillingBatchDto>> Handle(DeleteBillingBatchChargeCommand req, CancellationToken ct) =>
        BillingBatchEdit.ApplyAsync(tenantUow, req.Id,
            batch => tenantUow.Repository<BillingBatchCharge>().Delete(batch.RemoveCharge(req.ChargeId)), ct);
}
