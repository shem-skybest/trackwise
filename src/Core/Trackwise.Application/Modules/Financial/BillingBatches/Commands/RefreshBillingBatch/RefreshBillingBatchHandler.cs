using Trackwise.Application.Abstractions;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class RefreshBillingBatchHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RefreshBillingBatchCommand, Result<BillingBatchDto>>
{
    public Task<Result<BillingBatchDto>> Handle(RefreshBillingBatchCommand req, CancellationToken ct) =>
        BillingBatchEdit.ApplyAsync(tenantUow, req.Id, batch => BillingBatchSync.SyncAsync(tenantUow, batch, includeNew: true, ct), ct);
}
