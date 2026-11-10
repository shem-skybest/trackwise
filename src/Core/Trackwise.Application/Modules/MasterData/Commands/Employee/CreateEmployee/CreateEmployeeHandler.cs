using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Abstractions.Notifications;
using Trackwise.Application.Modules.IdentityAccess.Roles.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateEmployeeHandler(
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow,
    IMasterDataCodeGenerator codes,
    INotificationService notificationService,
    ITenantRoleAccess roleAccess)
    : IAppRequestHandler<CreateEmployeeCommand, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(CreateEmployeeCommand req, CancellationToken ct)
    {
        var existingEmployee = await tenantUow.Repository<Employee>().GetByIdAsync(req.UserId, ct);

        if (existingEmployee is not null)
        {
            return Result<EmployeeDto>.Fail("Employee already exists");
        }

        var user = await masterUow.Repository<User>().GetByIdAsync(req.UserId, ct);

        if (user is null)
        {
            return Result<EmployeeDto>.Fail("Could not find the specified user");
        }

        if (req.BranchId is { } branchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result<EmployeeDto>.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        var tenantRole = await tenantUow.Repository<TenantRole>().GetAsync(i => i.Name == req.Role, ct);
        if (tenantRole is not null && await roleAccess.CanAssignAsync(null, tenantRole, ct) is { IsSuccess: false } refused)
        {
            return Result<EmployeeDto>.Fail(refused.Error!);
        }

        var tenant = tenantUow.GetCurrentTenant();

        user.Tenant = tenant;
        var employee = Employee.CreateEmployeeFromUser(user, req.Salary, req.SalaryType);
        employee.Code = await codes.NextAsync(nameof(Employee), "EMP", ct: ct);
        employee.BranchId = req.BranchId;

        if (tenantRole is not null)
        {
            employee.Role = tenantRole;
        }

        await tenantUow.Repository<Employee>().AddAsync(employee, ct);
        if (tenantRole is not null)
        {
            await roleAccess.LogAssignmentAsync(employee, null, tenantRole, ct);
        }
        masterUow.Repository<User>().Update(user);

        await masterUow.SaveChangesAsync(ct);
        await tenantUow.SaveChangesAsync(ct);

        await notificationService.SendNotificationAsync("New Employee",
            $"A new employee '{employee.GetFullName()}' has joined. Role is '{tenantRole?.DisplayName}'");

        return Result<EmployeeDto>.Ok(employee.ToDto());
    }
}
