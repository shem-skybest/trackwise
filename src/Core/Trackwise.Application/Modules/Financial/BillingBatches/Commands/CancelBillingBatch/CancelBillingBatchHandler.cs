using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class CancelBillingBatchHandler(ITenantUnitOfWork tenantUow, ICurrentUserService currentUser)
    : IAppRequestHandler<CancelBillingBatchCommand, Result<BillingBatchDto>>
{
    public Task<Result<BillingBatchDto>> Handle(CancelBillingBatchCommand req, CancellationToken ct) =>
        BillingBatchEdit.ApplyAsync(tenantUow, req.Id, batch =>
        {
            var (trips, charges) = batch.Cancel(req.Reason, currentUser.GetUserName(), DateTime.UtcNow);
            foreach (var line in trips)
            {
                tenantUow.Repository<BillingBatchTrip>().Delete(line);
            }

            foreach (var charge in charges)
            {
                tenantUow.Repository<BillingBatchCharge>().Delete(charge);
            }
        }, ct);
}
