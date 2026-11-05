using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.Tenancy;
using Trackwise.Application.Abstractions.CurrentUser;

namespace Trackwise.Application.Modules.IdentityAccess.Features.Commands;

internal sealed class UpdateTenantFeatureHandler(
    IMasterUnitOfWork masterUow,
    ICurrentTenantAccessor tenantAccessor,
    ICurrentUserService currentUserService)
    : IAppRequestHandler<UpdateTenantFeatureCommand, Result>
{
    public async Task<Result> Handle(UpdateTenantFeatureCommand req, CancellationToken ct)
    {
        var tenant = tenantAccessor.GetCurrentTenant();

        // Get existing config or create new one
        var config = await masterUow.Repository<TenantFeatureConfig>()
            .GetAsync(c => c.TenantId == tenant.Id && c.Feature == req.Feature, ct);

        if (config is null)
        {
            config = new TenantFeatureConfig
            {
                TenantId = tenant.Id,
                Feature = req.Feature,
                IsEnabled = req.IsEnabled,
                IsAdminLocked = false,
                UpdatedBy = currentUserService.GetUserId().ToString(),
                UpdatedAt = DateTime.UtcNow
            };
            await masterUow.Repository<TenantFeatureConfig>().AddAsync(config, ct);
        }
        else
        {
            // Check if admin-locked
            if (config.IsAdminLocked)
            {
                var featureName = req.Feature.GetDescription();
                return Result.Fail($"The '{featureName}' feature is locked by an administrator and cannot be changed.");
            }

            config.IsEnabled = req.IsEnabled;
            config.UpdatedBy = currentUserService.GetUserId().ToString();
            config.UpdatedAt = DateTime.UtcNow;
            masterUow.Repository<TenantFeatureConfig>().Update(config);
        }

        await masterUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
