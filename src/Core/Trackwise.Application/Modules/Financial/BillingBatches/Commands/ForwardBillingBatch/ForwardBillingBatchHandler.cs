using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class ForwardBillingBatchHandler(ITenantUnitOfWork tenantUow, ICurrentUserService currentUser)
    : IAppRequestHandler<ForwardBillingBatchCommand, Result<BillingBatchDto>>
{
    public Task<Result<BillingBatchDto>> Handle(ForwardBillingBatchCommand req, CancellationToken ct)
    {
        var on = req.ForwardedOn ?? tenantUow.GetCurrentTenant().Settings.LocalDate(DateTime.UtcNow);
        return BillingBatchEdit.ApplyAsync(tenantUow, req.Id, batch => batch.Forward(on, currentUser.GetUserName()), ct);
    }
}
