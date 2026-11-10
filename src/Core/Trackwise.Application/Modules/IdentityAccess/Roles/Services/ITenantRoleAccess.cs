using Trackwise.Application.Abstractions.Common;
using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Services;

/// <summary>
/// The guardrails around changing access. Every path that edits a role or moves a person between
/// roles goes through here, so the rules hold no matter which screen or API made the change:
/// <list type="bullet">
/// <item>nobody grants, removes or hands out a permission they don't hold themselves;</item>
/// <item>nobody edits the role they are in, or changes their own role;</item>
/// <item>the Owner role is locked to the whole catalog.</item>
/// </list>
/// </summary>
public interface ITenantRoleAccess : IApplicationService
{
    Guid? CallerId { get; }

    Task<IReadOnlySet<string>> GetCallerPermissionsAsync(CancellationToken ct = default);

    /// <summary>The caller's own role, or null for a platform admin acting inside the tenant.</summary>
    Task<TenantRole?> GetCallerRoleAsync(CancellationToken ct = default);

    Task<IReadOnlySet<string>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>Whether the caller may change these permissions on this role.</summary>
    Task<Result> CanEditRoleAsync(TenantRole role, IEnumerable<string> changedPermissions, CancellationToken ct = default);

    /// <summary>Whether the caller may move <paramref name="employee"/> into <paramref name="newRole"/>.</summary>
    Task<Result> CanAssignAsync(Employee? employee, TenantRole newRole, CancellationToken ct = default);

    /// <summary>
    /// Writes the change-log row for moving the employee between roles. Assumes
    /// <see cref="CanAssignAsync"/> passed; the caller moves them, saves and invalidates the permission cache.
    /// </summary>
    Task LogAssignmentAsync(Employee employee, TenantRole? previousRole, TenantRole newRole, CancellationToken ct = default);

    void InvalidatePermissionCache();
}
