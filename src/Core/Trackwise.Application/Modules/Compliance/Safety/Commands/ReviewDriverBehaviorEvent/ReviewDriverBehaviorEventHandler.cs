using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.CurrentUser;

namespace Trackwise.Application.Modules.Compliance.Safety.Commands;

internal sealed class ReviewDriverBehaviorEventHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser)
    : IAppRequestHandler<ReviewDriverBehaviorEventCommand, Result<DriverBehaviorEventDto>>
{
    public async Task<Result<DriverBehaviorEventDto>> Handle(ReviewDriverBehaviorEventCommand req, CancellationToken ct)
    {
        var behaviorEvent = await tenantUow.Repository<DriverBehaviorEvent>().GetByIdAsync(req.Id, ct);

        if (behaviorEvent is null)
        {
            return Result<DriverBehaviorEventDto>.Fail($"Could not find driver behavior event with ID '{req.Id}'");
        }

        if (behaviorEvent.IsReviewed)
        {
            return Result<DriverBehaviorEventDto>.Fail("This event has already been reviewed.");
        }

        behaviorEvent.IsReviewed = true;
        behaviorEvent.ReviewedById = currentUser.GetUserId();
        behaviorEvent.ReviewedAt = DateTime.UtcNow;
        behaviorEvent.ReviewNotes = req.ReviewNotes;
        behaviorEvent.IsDismissed = req.IsDismissed;

        tenantUow.Repository<DriverBehaviorEvent>().Update(behaviorEvent);
        await tenantUow.SaveChangesAsync(ct);

        return Result<DriverBehaviorEventDto>.Ok(behaviorEvent.ToDto());
    }
}
