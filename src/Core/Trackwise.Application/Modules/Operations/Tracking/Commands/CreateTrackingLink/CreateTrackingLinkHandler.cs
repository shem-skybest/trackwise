using Trackwise.Application.Abstractions;
using Trackwise.Application.Utilities;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Options;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Options;
using Trackwise.Application.Abstractions.CurrentUser;

namespace Trackwise.Application.Modules.Operations.Tracking.Commands;

internal sealed class CreateTrackingLinkHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUserService,
    IOptions<CustomerPortalOptions> portalOptions)
    : IAppRequestHandler<CreateTrackingLinkCommand, Result<TrackingLinkDto>>
{
    private const int DefaultExpirationDays = 30;

    public async Task<Result<TrackingLinkDto>> Handle(CreateTrackingLinkCommand req, CancellationToken ct)
    {
        var currentUserId = currentUserService.GetUserId();
        if (currentUserId is null)
        {
            return Result<TrackingLinkDto>.Fail("User not authenticated.");
        }

        var load = await tenantUow.Repository<Load>().GetByIdAsync(req.LoadId, ct);
        if (load is null)
        {
            return Result<TrackingLinkDto>.Fail("Load not found.");
        }

        var trackingLink = new TrackingLink
        {
            Token = TokenGenerator.GenerateSecureToken(64),
            LoadId = req.LoadId,
            ExpiresAt = DateTime.UtcNow.AddDays(DefaultExpirationDays),
            CreatedByUserId = currentUserId.Value
        };

        await tenantUow.Repository<TrackingLink>().AddAsync(trackingLink, ct);
        await tenantUow.SaveChangesAsync(ct);

        var tenant = tenantUow.GetCurrentTenant();

        return Result<TrackingLinkDto>.Ok(new TrackingLinkDto
        {
            Id = trackingLink.Id,
            Token = trackingLink.Token,
            Url = $"{portalOptions.Value.BaseUrl}/track/{tenant.Id}/{trackingLink.Token}",
            LoadId = load.Id,
            LoadNumber = load.Number,
            LoadName = load.Name,
            ExpiresAt = trackingLink.ExpiresAt,
            IsActive = trackingLink.IsActive,
            IsExpired = false,
            AccessCount = 0,
            LastAccessedAt = null,
            CreatedAt = trackingLink.CreatedAt
        });
    }
}
