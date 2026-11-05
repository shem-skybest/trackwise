using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class MarkBillingBatchBilledHandler(ITenantUnitOfWork tenantUow, ICurrentUserService currentUser)
    : IAppRequestHandler<MarkBillingBatchBilledCommand, Result<BillingBatchDto>>
{
    public Task<Result<BillingBatchDto>> Handle(MarkBillingBatchBilledCommand req, CancellationToken ct)
    {
        var on = req.BilledOn ?? tenantUow.GetCurrentTenant().Settings.LocalDate(DateTime.UtcNow);
        return BillingBatchEdit.ApplyAsync(tenantUow, req.Id,
            batch => batch.MarkBilled(req.InvoiceNumber, on, currentUser.GetUserName()), ct);
    }
}
