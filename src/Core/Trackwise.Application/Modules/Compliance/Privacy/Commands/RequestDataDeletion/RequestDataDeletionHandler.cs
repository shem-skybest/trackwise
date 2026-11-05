using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions.CurrentUser;

namespace Trackwise.Application.Modules.Compliance.Privacy.Commands;

internal sealed class RequestDataDeletionHandler(
    IMasterUnitOfWork masterUow,
    ICurrentUserService currentUserService)
    : IAppRequestHandler<RequestDataDeletionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RequestDataDeletionCommand req, CancellationToken ct)
    {
        var userId = currentUserService.GetUserId();
        if (userId is null)
        {
            return Result<Guid>.Fail("User not authenticated.");
        }

        var existingPending = await masterUow.Repository<DataDeletionRequest>()
            .Query()
            .AnyAsync(r => r.UserId == userId.Value && r.Status == DataDeletionStatus.Pending, ct);

        if (existingPending)
        {
            return Result<Guid>.Fail("A deletion request is already pending. Cancel it first to submit a new one.");
        }

        var user = await masterUow.Repository<User>().GetByIdAsync(userId.Value, ct);
        if (user is null)
        {
            return Result<Guid>.Fail("User not found.");
        }

        var now = DateTime.UtcNow;
        var request = new DataDeletionRequest
        {
            UserId = userId.Value,
            RequestedAt = now,
            ScheduledFor = now + PrivacyDefaults.DeletionGracePeriod,
            Reason = req.Reason,
            Status = DataDeletionStatus.Pending
        };

        user.DeletionRequestedAt = now;

        await masterUow.Repository<DataDeletionRequest>().AddAsync(request, ct);
        await masterUow.SaveChangesAsync(ct);

        return Result<Guid>.Ok(request.Id);
    }
}
