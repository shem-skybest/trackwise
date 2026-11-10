using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.IdentityAccess.Roles.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Identity.Claims;
using Trackwise.Shared.Identity.Roles;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Queries;

internal sealed class GetTenantRolesHandler(ITenantUnitOfWork tenantUow, ITenantRoleAccess roleAccess)
    : IAppRequestHandler<GetTenantRolesQuery, PagedResult<RoleDto>>
{
    public async Task<PagedResult<RoleDto>> Handle(GetTenantRolesQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<TenantRole>().Query();

        if (!string.IsNullOrEmpty(req.Search))
        {
            query = query.Where(i =>
                i.Name.Contains(req.Search) ||
                (i.DisplayName != null && i.DisplayName.Contains(req.Search)));
        }

        var totalItems = query.Count();
        var roles = query.ApplyPaging(req.Page, req.PageSize).ToList();
        var roleIds = roles.Select(r => r.Id).ToList();

        // One read for every role's claims and one for head counts, instead of a lazy load per row.
        var permissionsByRole = tenantUow.Repository<TenantRoleClaim>().Query()
            .Where(c => roleIds.Contains(c.RoleId) && c.ClaimType == CustomClaimTypes.Permission)
            .Select(c => new { c.RoleId, c.ClaimValue })
            .AsEnumerable()
            .GroupBy(c => c.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(c => c.ClaimValue).ToHashSet());

        var employeesByRole = tenantUow.Repository<Employee>().Query()
            .Where(e => e.RoleId != null && roleIds.Contains(e.RoleId.Value))
            .GroupBy(e => e.RoleId!.Value)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionary(g => g.RoleId, g => g.Count);

        var callerPermissions = await roleAccess.GetCallerPermissionsAsync(ct);
        var callerRole = await roleAccess.GetCallerRoleAsync(ct);
        var canManageRoles = callerPermissions.Contains(Shared.Identity.Policies.Permission.TenantRole.Manage);
        var canManageEmployees = callerPermissions.Contains(Shared.Identity.Policies.Permission.Employee.Manage);

        var items = roles.Select(role =>
        {
            var permissions = permissionsByRole.GetValueOrDefault(role.Id) ?? [];
            var dto = role.ToDto();
            dto.IsBuiltIn = TenantRoles.IsBuiltIn(role.Name);
            dto.IsCustomized = role.IsCustomized;
            dto.PermissionCount = permissions.Count;
            dto.EmployeeCount = employeesByRole.GetValueOrDefault(role.Id);

            // Mirrors ITenantRoleAccess; the API re-checks on every write, this only shapes the UI.
            dto.CanEdit = canManageRoles
                          && role.Name is not (TenantRoles.Owner or TenantRoles.Customer)
                          && role.Id != callerRole?.Id;
            dto.CanAssign = canManageEmployees
                            && role.Name != TenantRoles.Customer
                            && permissions.IsSubsetOf(callerPermissions);
            return dto;
        }).ToArray();

        return PagedResult<RoleDto>.Ok(items, totalItems, req.PageSize);
    }
}
