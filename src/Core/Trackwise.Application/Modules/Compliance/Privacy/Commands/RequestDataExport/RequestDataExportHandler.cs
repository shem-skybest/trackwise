using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions.CurrentUser;

namespace Trackwise.Application.Modules.Compliance.Privacy.Commands;

internal sealed class RequestDataExportHandler(
    IMasterUnitOfWork masterUow,
    ICurrentUserService currentUserService)
    : IAppRequestHandler<RequestDataExportCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RequestDataExportCommand req, CancellationToken ct)
    {
        var userId = currentUserService.GetUserId();
        if (userId is null)
        {
            return Result<Guid>.Fail("User not authenticated.");
        }

        var since = DateTime.UtcNow - TimeSpan.FromDays(1);
        var recentCount = await masterUow.Repository<DataExportRequest>()
            .Query()
            .Where(r => r.UserId == userId.Value && r.RequestedAt >= since)
            .CountAsync(ct);

        if (recentCount >= PrivacyDefaults.ExportRateLimitPerDay)
        {
            return Result<Guid>.Fail(
                $"You can request at most {PrivacyDefaults.ExportRateLimitPerDay} data export per 24 hours.");
        }

        var request = new DataExportRequest
        {
            UserId = userId.Value,
            Status = DataExportStatus.Pending
        };

        await masterUow.Repository<DataExportRequest>().AddAsync(request, ct);
        await masterUow.SaveChangesAsync(ct);

        return Result<Guid>.Ok(request.Id);
    }
}
