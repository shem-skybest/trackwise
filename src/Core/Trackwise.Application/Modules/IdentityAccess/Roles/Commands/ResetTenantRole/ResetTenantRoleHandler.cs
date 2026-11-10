using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.IdentityAccess.Roles.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Identity.Roles;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

internal sealed class ResetTenantRoleHandler(
    ITenantUnitOfWork tenantUow,
    ITenantRoleAccess roleAccess,
    ITenantRolePermissionWriter writer)
    : IAppRequestHandler<ResetTenantRoleCommand, Result>
{
    public async Task<Result> Handle(ResetTenantRoleCommand req, CancellationToken ct)
    {
        var role = await tenantUow.Repository<TenantRole>().GetAsync(r => r.Name == req.RoleName, ct);
        if (role is null)
        {
            return Result.Fail($"Could not find role '{req.RoleName}'");
        }

        if (!TenantRoles.IsBuiltIn(role.Name))
        {
            return Result.Fail("Only built-in roles have defaults to go back to.");
        }

        var result = await writer.ReplaceAsync(role, TenantRolePermissions.GetDefaultsForRole(role.Name), ct);
        if (!result.IsSuccess)
        {
            return result;
        }

        role.IsCustomized = false;
        await writer.LogAsync(role, TenantRoleChangeType.ResetToDefaults, null, ct);
        await tenantUow.SaveChangesAsync(ct);
        roleAccess.InvalidatePermissionCache();
        return Result.Ok();
    }
}
