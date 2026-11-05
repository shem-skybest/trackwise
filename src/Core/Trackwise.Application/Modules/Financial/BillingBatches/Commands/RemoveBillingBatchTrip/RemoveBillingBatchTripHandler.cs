using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class RemoveBillingBatchTripHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RemoveBillingBatchTripCommand, Result<BillingBatchDto>>
{
    public Task<Result<BillingBatchDto>> Handle(RemoveBillingBatchTripCommand req, CancellationToken ct) =>
        BillingBatchEdit.ApplyAsync(tenantUow, req.Id, batch =>
        {
            var (line, charges) = batch.RemoveTrip(req.TripId);
            tenantUow.Repository<BillingBatchTrip>().Delete(line);
            foreach (var charge in charges)
            {
                tenantUow.Repository<BillingBatchCharge>().Delete(charge);
            }
        }, ct);
}
