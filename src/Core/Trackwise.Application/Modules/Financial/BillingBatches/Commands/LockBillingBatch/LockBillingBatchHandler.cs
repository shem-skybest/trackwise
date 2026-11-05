using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class LockBillingBatchHandler(ITenantUnitOfWork tenantUow, ICurrentUserService currentUser)
    : IAppRequestHandler<LockBillingBatchCommand, Result<BillingBatchDto>>
{
    public Task<Result<BillingBatchDto>> Handle(LockBillingBatchCommand req, CancellationToken ct) =>
        BillingBatchEdit.ApplyAsync(tenantUow, req.Id, async batch =>
        {
            var trips = await BillingBatchSync.CurrentTripsAsync(tenantUow, batch, ct);
            batch.Lock(trips, currentUser.GetUserName(), DateTime.UtcNow);
        }, ct);
}
