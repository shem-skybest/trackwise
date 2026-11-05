using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class UnlockBillingBatchHandler(ITenantUnitOfWork tenantUow, ICurrentUserService currentUser)
    : IAppRequestHandler<UnlockBillingBatchCommand, Result<BillingBatchDto>>
{
    public Task<Result<BillingBatchDto>> Handle(UnlockBillingBatchCommand req, CancellationToken ct) =>
        BillingBatchEdit.ApplyAsync(tenantUow, req.Id,
            batch => batch.Unlock(req.Reason, currentUser.GetUserName(), DateTime.UtcNow), ct);
}
