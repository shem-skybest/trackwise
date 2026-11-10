using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.IdentityAccess.Roles.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

internal sealed class UpdateTenantRolePermissionsHandler(
    ITenantUnitOfWork tenantUow,
    ITenantRoleAccess roleAccess,
    ITenantRolePermissionWriter writer)
    : IAppRequestHandler<UpdateTenantRolePermissionsCommand, Result>
{
    public async Task<Result> Handle(UpdateTenantRolePermissionsCommand req, CancellationToken ct)
    {
        var role = await tenantUow.Repository<TenantRole>().GetAsync(r => r.Name == req.RoleName, ct);
        if (role is null)
        {
            return Result.Fail($"Could not find role '{req.RoleName}'");
        }

        var result = await writer.ReplaceAsync(role, req.Permissions, ct);
        if (!result.IsSuccess)
        {
            return result;
        }

        role.IsCustomized = true;
        await tenantUow.SaveChangesAsync(ct);
        roleAccess.InvalidatePermissionCache();
        return Result.Ok();
    }
}
