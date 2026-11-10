using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.IdentityAccess.Roles.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateEmployeeHandler(ITenantUnitOfWork tenantUow, ITenantRoleAccess roleAccess)
    : IAppRequestHandler<UpdateEmployeeCommand, Result>
{
    public async Task<Result> Handle(UpdateEmployeeCommand req, CancellationToken ct)
    {
        var employee = await tenantUow.Repository<Employee>().GetByIdAsync(req.Id, ct);

        if (employee is null)
        {
            return Result.Fail($"Could not find employee with ID '{req.Id}'");
        }

        if (req.BranchId is { } branchId && branchId != employee.BranchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        if (!string.IsNullOrWhiteSpace(req.FirstName))
        {
            employee.FirstName = req.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(req.LastName))
        {
            employee.LastName = req.LastName;
        }

        if (req.JobTitle is not null)
        {
            employee.JobTitle = string.IsNullOrWhiteSpace(req.JobTitle) ? null : req.JobTitle.Trim();
        }

        // A role change here obeys the same guardrails as the dedicated assign endpoint - otherwise
        // this edit form would be a way around them for anyone holding MasterData.Manage.
        var tenantRole = await tenantUow.Repository<TenantRole>().GetAsync(i => i.Name == req.Role, ct);
        var roleChanged = tenantRole is not null && tenantRole.Id != employee.RoleId;

        if (roleChanged)
        {
            var check = await roleAccess.CanAssignAsync(employee, tenantRole!, ct);
            if (!check.IsSuccess)
            {
                return check;
            }

            await roleAccess.LogAssignmentAsync(employee, employee.Role, tenantRole!, ct);
            employee.Role = tenantRole;
        }

        if (req.SalaryType.HasValue && employee.SalaryType != req.SalaryType)
        {
            employee.SalaryType = req.SalaryType.Value;
        }

        if (req.Salary.HasValue && employee.Salary != req.Salary)
        {
            var salaryAmount = req.SalaryType == SalaryType.None ? 0 : req.Salary.Value;
            employee.Salary = new() { Amount = salaryAmount, Currency = employee.Salary.Currency };
        }

        if (req.Address is not null)
        {
            employee.Address = req.Address;
        }

        employee.EmploymentType = req.EmploymentType ?? employee.EmploymentType;
        employee.HomeUnit = req.HomeUnit ?? employee.HomeUnit;
        employee.BranchId = req.BranchId;
        employee.IsActive = req.IsActive;

        // Status is set last: its setter force-deactivates on Terminated (Employee.cs), and that
        // has to be the final word over whatever IsActive the caller sent - setting it earlier let
        // a default `IsActive: true` payload silently undo the termination side effect.
        if (req.Status.HasValue && employee.Status != req.Status)
        {
            employee.Status = req.Status.Value;
        }

        await tenantUow.SaveChangesAsync(ct);
        if (roleChanged)
        {
            roleAccess.InvalidatePermissionCache();
        }

        return Result.Ok();
    }
}
