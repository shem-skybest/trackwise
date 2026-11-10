using Trackwise.Application.Abstractions.Common;

namespace Trackwise.Application.Modules.IdentityAccess.Users.Services;

/// <summary>
/// The caller's effective permissions, cached per user and tenant. Resolving them costs ~9 queries
/// across both databases, so consumers come through here, never through the query directly.
/// </summary>
public interface IUserPermissionService : IApplicationService
{
    Task<IReadOnlySet<string>> GetPermissionsAsync(
        Guid userId, Guid? tenantId, CancellationToken ct = default);

    /// <summary>
    /// Drops every cached permission set for the tenant. Call after changing a role's permissions
    /// or moving someone to another role, or the change waits out the cache expiry.
    /// </summary>
    void InvalidateTenant(Guid tenantId);

    Task<bool> HasPermissionAsync(
        Guid userId, Guid? tenantId, string permission, CancellationToken ct = default);
}
