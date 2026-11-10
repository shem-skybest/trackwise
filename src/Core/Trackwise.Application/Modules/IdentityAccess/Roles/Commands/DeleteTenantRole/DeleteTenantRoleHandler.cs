using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.IdentityAccess.Roles.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Roles;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

internal sealed class DeleteTenantRoleHandler(
    ITenantUnitOfWork tenantUow,
    ITenantRoleAccess roleAccess,
    ITenantRolePermissionWriter writer)
    : IAppRequestHandler<DeleteTenantRoleCommand, Result>
{
    public async Task<Result> Handle(DeleteTenantRoleCommand req, CancellationToken ct)
    {
        var role = await tenantUow.Repository<TenantRole>().GetAsync(r => r.Name == req.RoleName, ct);
        if (role is null)
        {
            return Result.Fail($"Could not find role '{req.RoleName}'");
        }

        if (TenantRoles.IsBuiltIn(role.Name))
        {
            return Result.Fail("Built-in roles can't be deleted. Reset it to its defaults instead.");
        }

        // Counted over every employee, inactive ones included: deleting the role would leave them
        // with no access at all if they are reactivated.
        var inUse = await tenantUow.Repository<Employee>().CountAsync(e => e.Role != null && e.Role.Id == role.Id, ct);
        if (inUse > 0)
        {
            return Result.Fail($"{inUse} people are still in '{role.DisplayName}'. Move them to another role first.");
        }

        var permissions = await roleAccess.GetRolePermissionsAsync(role.Id, ct);
        var check = await roleAccess.CanEditRoleAsync(role, permissions, ct);
        if (!check.IsSuccess)
        {
            return check;
        }

        await writer.LogAsync(role, TenantRoleChangeType.RoleDeleted, null, ct);
        tenantUow.Repository<TenantRole>().Delete(role);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
