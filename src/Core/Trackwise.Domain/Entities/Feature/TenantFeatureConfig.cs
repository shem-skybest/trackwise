using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Represents a feature toggle configuration for a specific tenant.
/// </summary>
public class TenantFeatureConfig : AuditableEntity, IMasterEntity
{
    public Guid TenantId { get; set; }

    /// <summary>
    /// The feature being configured.
    /// </summary>
    public TenantFeature Feature { get; set; }

    /// <summary>
    /// Whether the feature is enabled for this tenant.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// If true, only an admin can change this setting (tenant owner cannot override).
    /// </summary>
    public bool IsAdminLocked { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
}
