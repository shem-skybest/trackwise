using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.IdentityAccess.Users.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Claims;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Identity.Roles;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Services;

internal sealed class TenantRoleAccess(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser,
    IUserPermissionService userPermissions) : ITenantRoleAccess
{
    public Guid? CallerId => currentUser.GetUserId();

    public async Task<IReadOnlySet<string>> GetCallerPermissionsAsync(CancellationToken ct = default)
    {
        if (CallerId is not { } callerId)
        {
            return new HashSet<string>();
        }

        return await userPermissions.GetPermissionsAsync(callerId, tenantUow.GetCurrentTenant().Id, ct);
    }

    public async Task<TenantRole?> GetCallerRoleAsync(CancellationToken ct = default)
    {
        if (CallerId is not { } callerId)
        {
            return null;
        }

        var caller = await tenantUow.Repository<Employee>().GetByIdAsync(callerId, ct);
        return caller?.Role;
    }

    public async Task<IReadOnlySet<string>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default)
    {
        var claims = await tenantUow.Repository<TenantRoleClaim>()
            .GetListAsync(c => c.RoleId == roleId && c.ClaimType == CustomClaimTypes.Permission, ct);
        return claims.Select(c => c.ClaimValue).ToHashSet();
    }

    public async Task<Result> CanEditRoleAsync(
        TenantRole role, IEnumerable<string> changedPermissions, CancellationToken ct = default)
    {
        if (role.Name == TenantRoles.Owner)
        {
            return Result.Fail("The Owner role always has every permission and can't be edited.");
        }

        if (role.Name == TenantRoles.Customer)
        {
            return Result.Fail("The Customer role belongs to the customer portal and can't be edited here.");
        }

        var callerRole = await GetCallerRoleAsync(ct);
        if (callerRole?.Id == role.Id)
        {
            return Result.Fail("You can't edit the role you are in. Ask the Owner or another admin.");
        }

        var callerPermissions = await GetCallerPermissionsAsync(ct);
        var outOfReach = changedPermissions.Where(p => !callerPermissions.Contains(p)).ToList();
        if (outOfReach.Count > 0)
        {
            return Result.Fail(
                $"You can only add or remove permissions you have yourself. Not yours: {string.Join(", ", outOfReach)}");
        }

        return Result.Ok();
    }

    public async Task<Result> CanAssignAsync(Employee? employee, TenantRole newRole, CancellationToken ct = default)
    {
        if (employee is not null && employee.Id == CallerId)
        {
            return Result.Fail("You can't change your own role. Ask the Owner or another admin.");
        }

        if (newRole.Name == TenantRoles.Customer)
        {
            return Result.Fail("Customer is the portal role; staff can't be placed in it.");
        }

        var callerPermissions = await GetCallerPermissionsAsync(ct);
        if (!callerPermissions.Contains(Permission.Employee.Manage))
        {
            return Result.Fail("Only people who manage employees can assign roles.");
        }

        var newRolePermissions = await GetRolePermissionsAsync(newRole.Id, ct);
        if (!newRolePermissions.IsSubsetOf(callerPermissions))
        {
            return Result.Fail(
                $"'{newRole.DisplayName ?? newRole.Name}' has permissions you don't have, so you can't give it to anyone.");
        }

        // Without this, someone could demote a person who outranks them - the Owner included.
        if (employee?.Role is { } currentRole && currentRole.Id != newRole.Id)
        {
            var currentRolePermissions = await GetRolePermissionsAsync(currentRole.Id, ct);
            if (!currentRolePermissions.IsSubsetOf(callerPermissions))
            {
                return Result.Fail(
                    $"{employee.GetFullName()} is in '{currentRole.DisplayName ?? currentRole.Name}', which has permissions you don't have, so you can't move them.");
            }
        }

        return Result.Ok();
    }

    public async Task LogAssignmentAsync(
        Employee employee, TenantRole? previousRole, TenantRole newRole, CancellationToken ct = default)
    {
        await tenantUow.Repository<TenantRoleChangeLog>().AddAsync(new TenantRoleChangeLog
        {
            ChangeType = TenantRoleChangeType.EmployeeAssigned,
            RoleName = newRole.Name,
            RoleDisplayName = newRole.DisplayName,
            EmployeeId = employee.Id,
            EmployeeName = employee.GetFullName(),
            PreviousRoleName = previousRole?.DisplayName ?? previousRole?.Name,
            ChangedById = CallerId,
            ChangedByName = currentUser.GetUserName()
        }, ct);
    }

    public void InvalidatePermissionCache() =>
        userPermissions.InvalidateTenant(tenantUow.GetCurrentTenant().Id);
}
