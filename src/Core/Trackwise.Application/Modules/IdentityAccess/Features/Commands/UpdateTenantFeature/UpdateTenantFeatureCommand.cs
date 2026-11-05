using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.IdentityAccess.Features.Commands;

/// <summary>
/// Command to toggle a single feature for the current tenant (used by tenant owner).
/// </summary>
public class UpdateTenantFeatureCommand : ICommand
{
    /// <summary>
    /// The feature to toggle.
    /// </summary>
    public TenantFeature Feature { get; set; }

    /// <summary>
    /// Whether to enable or disable the feature.
    /// </summary>
    public bool IsEnabled { get; set; }
}
