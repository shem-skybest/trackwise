using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Features.Queries;

/// <summary>
/// Query to get all feature statuses for a tenant.
/// </summary>
public class GetTenantFeaturesQuery : IQuery<Result<IReadOnlyList<FeatureStatusDto>>>
{
    /// <summary>
    /// The tenant ID. If null, uses the current tenant from context.
    /// </summary>
    public Guid? TenantId { get; set; }
}
