using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Claims;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Services;

internal sealed class TenantRolePermissionWriter(
    ITenantUnitOfWork tenantUow,
    ITenantRoleAccess roleAccess,
    ICurrentUserService currentUser) : ITenantRolePermissionWriter
{
    public async Task<Result> ReplaceAsync(TenantRole role, IEnumerable<string> target, CancellationToken ct)
    {
        // Basic permissions keep every signed-in screen working; no role may lose them.
        var wanted = target.Concat(TenantRolePermissions.GetBasicPermissions()).ToHashSet();
        var claims = await tenantUow.Repository<TenantRoleClaim>()
            .GetListAsync(c => c.RoleId == role.Id && c.ClaimType == CustomClaimTypes.Permission, ct);
        var current = claims.Select(c => c.ClaimValue).ToHashSet();

        var added = wanted.Except(current).ToList();
        var removed = claims.Where(c => !wanted.Contains(c.ClaimValue)).ToList();

        var check = await roleAccess.CanEditRoleAsync(role, added.Concat(removed.Select(c => c.ClaimValue)), ct);
        if (!check.IsSuccess)
        {
            return check;
        }

        foreach (var permission in added)
        {
            await tenantUow.Repository<TenantRoleClaim>().AddAsync(
                new TenantRoleClaim(CustomClaimTypes.Permission, permission) { RoleId = role.Id }, ct);
            await LogAsync(role, TenantRoleChangeType.PermissionAdded, permission, ct);
        }

        foreach (var claim in removed)
        {
            tenantUow.Repository<TenantRoleClaim>().Delete(claim);
            await LogAsync(role, TenantRoleChangeType.PermissionRemoved, claim.ClaimValue, ct);
        }

        return Result.Ok();
    }

    public Task LogAsync(TenantRole role, TenantRoleChangeType type, string? permission, CancellationToken ct) =>
        tenantUow.Repository<TenantRoleChangeLog>().AddAsync(new TenantRoleChangeLog
        {
            ChangeType = type,
            RoleName = role.Name,
            RoleDisplayName = role.DisplayName,
            Permission = permission,
            ChangedById = currentUser.GetUserId(),
            ChangedByName = currentUser.GetUserName()
        }, ct);
}
