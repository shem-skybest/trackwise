using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Tracking.Commands;

internal sealed class RevokeTrackingLinkHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RevokeTrackingLinkCommand, Result>
{
    public async Task<Result> Handle(RevokeTrackingLinkCommand req, CancellationToken ct)
    {
        var trackingLink = await tenantUow.Repository<TrackingLink>().GetByIdAsync(req.Id, ct);

        if (trackingLink is null)
        {
            return Result.Fail("Tracking link not found.");
        }

        trackingLink.IsActive = false;
        tenantUow.Repository<TrackingLink>().Update(trackingLink);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
