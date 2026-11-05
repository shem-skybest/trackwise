using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Represents the default feature configuration applied to new tenants.
/// </summary>
public class DefaultFeatureConfig : Entity, IMasterEntity
{
    /// <summary>
    /// The feature being configured.
    /// </summary>
    public TenantFeature Feature { get; set; }

    /// <summary>
    /// Whether this feature is enabled by default for new tenants.
    /// </summary>
    public bool IsEnabledByDefault { get; set; } = true;
}
