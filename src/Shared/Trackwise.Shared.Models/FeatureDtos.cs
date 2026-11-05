using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// Represents the status of a feature for a tenant.
/// </summary>
public record FeatureStatusDto(
    TenantFeature Feature,
    string Name,
    bool IsEnabled,
    bool IsAdminLocked,
    bool IsIncludedInPlan);

/// <summary>
/// Represents the default configuration for a feature.
/// </summary>
public record DefaultFeatureStatusDto(
    TenantFeature Feature,
    string Name,
    bool IsEnabledByDefault);
