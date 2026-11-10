using Trackwise.Application.Abstractions.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Services;

/// <summary>
/// Diffs a role's permissions against a target set, checks the diff against the caller's reach
/// (<see cref="ITenantRoleAccess.CanEditRoleAsync"/>), and writes the claim rows plus one change-log
/// row per permission. Shared by edit, copy and reset so all three obey the same guardrails and
/// leave the same trail. Does not save.
/// </summary>
public interface ITenantRolePermissionWriter : IApplicationService
{
    Task<Result> ReplaceAsync(TenantRole role, IEnumerable<string> target, CancellationToken ct);

    Task LogAsync(TenantRole role, TenantRoleChangeType type, string? permission, CancellationToken ct);
}
