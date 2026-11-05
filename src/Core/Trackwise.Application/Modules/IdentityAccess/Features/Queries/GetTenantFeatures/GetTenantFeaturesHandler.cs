using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.Features;
using Trackwise.Application.Abstractions.Tenancy;

namespace Trackwise.Application.Modules.IdentityAccess.Features.Queries;

internal sealed class GetTenantFeaturesHandler(
    IFeatureService featureService,
    ICurrentTenantAccessor tenantAccessor)
    : IAppRequestHandler<GetTenantFeaturesQuery, Result<IReadOnlyList<FeatureStatusDto>>>
{
    public async Task<Result<IReadOnlyList<FeatureStatusDto>>> Handle(
        GetTenantFeaturesQuery req,
        CancellationToken ct)
    {
        var tenantId = req.TenantId ?? tenantAccessor.GetCurrentTenant().Id;
        var features = await featureService.GetAllFeatureStatusAsync(tenantId);
        return Result<IReadOnlyList<FeatureStatusDto>>.Ok(features);
    }
}
