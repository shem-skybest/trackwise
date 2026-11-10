using Trackwise.Application.Modules.IdentityAccess.Roles.Services;
using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// <see cref="Employee.Status"/>'s setter force-deactivates on <see cref="EmployeeStatus.Terminated"/>
/// (see <c>Employee.cs</c>) - the confirmed design decision that termination always deactivates the
/// master record. <see cref="UpdateEmployeeHandler"/> used to apply that assignment before the
/// unconditional <c>employee.IsActive = req.IsActive</c>, so a request that left <c>IsActive</c> at
/// its default (<c>true</c>) - which every caller did, since the Angular form had no toggle for it
/// until this promotion's frontend shipped - silently re-activated the record the same save that
/// terminated it. These pin the fixed ordering.
/// </summary>
public class UpdateEmployeeHandlerTests
{
    private static Employee NewActiveEmployee() => new()
    {
        Code = "EMP-100",
        Email = "worker@example.com",
        FirstName = "Worker",
        LastName = "One",
        Status = EmployeeStatus.Active,
        IsActive = true
    };

    private static (UpdateEmployeeHandler sut, ITenantUnitOfWork tenantUow) NewSut(Employee employee)
    {
        var tenantUow = Substitute.For<ITenantUnitOfWork>();
        var employeeRepo = Substitute.For<ITenantRepository<Employee, Guid>>();
        var roleRepo = Substitute.For<ITenantRepository<TenantRole, Guid>>();

        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.Repository<TenantRole>().Returns(roleRepo);
        employeeRepo.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        roleRepo.GetAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantRole, bool>>>(),
            Arg.Any<CancellationToken>()).Returns((TenantRole?)null);

        return (new UpdateEmployeeHandler(tenantUow, Substitute.For<ITenantRoleAccess>()), tenantUow);
    }

    [Fact]
    public async Task TerminatingAnEmployee_ForcesIsActiveFalse_EvenWhenTheRequestDefaultsItTrue()
    {
        var employee = NewActiveEmployee();
        var (sut, _) = NewSut(employee);

        // IsActive left at the command's own default (true) - the point being pinned is that
        // Status still wins over it.
        var result = await sut.Handle(
            new UpdateEmployeeCommand { Id = employee.Id, Status = EmployeeStatus.Terminated }, default);

        Assert.True(result.IsSuccess);
        Assert.False(employee.IsActive);
    }

    [Fact]
    public async Task SettingEmployeeOnLeave_LeavesIsActiveAtWhateverTheRequestSent()
    {
        // OnLeave/Suspended deliberately never touch IsActive (Employee.cs) - unlike Terminated,
        // an explicit false here must stick, not get silently reverted to true.
        var employee = NewActiveEmployee();
        var (sut, _) = NewSut(employee);

        var result = await sut.Handle(
            new UpdateEmployeeCommand
            {
                Id = employee.Id,
                Status = EmployeeStatus.OnLeave,
                IsActive = false
            }, default);

        Assert.True(result.IsSuccess);
        Assert.False(employee.IsActive);
    }

    [Fact]
    public async Task RehiringATerminatedEmployee_DoesNotAutoReactivate()
    {
        // Un-terminating (Status back to Active) is deliberately not a mirror of terminating - a
        // steward must flip IsActive back on as its own, separate action (Employee.cs doc comment).
        var employee = NewActiveEmployee();
        employee.Status = EmployeeStatus.Terminated;
        employee.IsActive = false;

        var (sut, _) = NewSut(employee);

        var result = await sut.Handle(
            new UpdateEmployeeCommand
            {
                Id = employee.Id,
                Status = EmployeeStatus.Active,
                IsActive = false
            }, default);

        Assert.True(result.IsSuccess);
        Assert.False(employee.IsActive);
    }
}
