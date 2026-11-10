using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.IdentityAccess.Roles.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

internal sealed class AssignEmployeeRoleHandler(ITenantUnitOfWork tenantUow, ITenantRoleAccess roleAccess)
    : IAppRequestHandler<AssignEmployeeRoleCommand, Result>
{
    public async Task<Result> Handle(AssignEmployeeRoleCommand req, CancellationToken ct)
    {
        var employee = await tenantUow.Repository<Employee>().GetByIdAsync(req.EmployeeId, ct);
        if (employee is null)
        {
            return Result.Fail($"Could not find employee with ID '{req.EmployeeId}'");
        }

        var role = await tenantUow.Repository<TenantRole>().GetAsync(r => r.Name == req.RoleName, ct);
        if (role is null)
        {
            return Result.Fail($"Could not find role '{req.RoleName}'");
        }

        var previousRole = employee.Role;
        if (previousRole?.Id == role.Id)
        {
            return Result.Ok();
        }

        var check = await roleAccess.CanAssignAsync(employee, role, ct);
        if (!check.IsSuccess)
        {
            return check;
        }

        employee.Role = role;
        await roleAccess.LogAssignmentAsync(employee, previousRole, role, ct);
        await tenantUow.SaveChangesAsync(ct);
        roleAccess.InvalidatePermissionCache();
        return Result.Ok();
    }
}
