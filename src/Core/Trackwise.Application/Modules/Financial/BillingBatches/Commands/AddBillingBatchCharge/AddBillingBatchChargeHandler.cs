using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class AddBillingBatchChargeHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<AddBillingBatchChargeCommand, Result<BillingBatchDto>>
{
    public Task<Result<BillingBatchDto>> Handle(AddBillingBatchChargeCommand req, CancellationToken ct) =>
        BillingBatchEdit.ApplyAsync(tenantUow, req.Id, async batch =>
        {
            var charge = batch.AddCharge(new BillingChargeSpec(req.Kind, req.Description, req.Amount, req.IsVatable, req.TripId));
            await tenantUow.Repository<BillingBatchCharge>().AddAsync(charge, ct);
        }, ct);
}
