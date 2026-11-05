using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.IdentityAccess.Users.Queries;
using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.AspNetCore.Identity;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Employee's id doubles as a live platform login's id, which is what makes this promotion different
/// from every other one: a data-hygiene operation (soft-delete, merge) can silently deauthenticate a
/// currently logged-in person as a side effect. These pin both halves of the guarantee - that a
/// soft-deleted employee already loses tenant access the same way a hard-deleted one always has (no
/// code change needed, see <c>GetCurrentUserPermissionsHandler</c>'s own comment), and that Delete and
/// Merge refuse to touch a still-live login without an explicit, reasoned confirmation.
/// </summary>
public class EmployeePermissionResolutionTests
{
    private static UserManager<User> NewUserManager(User? user)
    {
        var store = Substitute.For<IUserStore<User>>();
        var userManager = Substitute.For<UserManager<User>>(
            store, null, null, null, null, null, null, null, null);

        userManager.FindByIdAsync(Arg.Any<string>()).Returns(user);
        userManager.GetRolesAsync(Arg.Any<User>()).Returns(new List<string>());

        return userManager;
    }

    private static RoleManager<AppRole> NewRoleManager()
    {
        var store = Substitute.For<IRoleStore<AppRole>>();
        return Substitute.For<RoleManager<AppRole>>(store, null, null, null, null);
    }

    #region GetCurrentUserPermissionsHandler

    [Fact]
    public async Task SoftDeletedEmployee_ResolvesToZeroTenantPermissions()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var user = new User { Id = userId, FirstName = "Juan", LastName = "Dela Cruz" };

        var tenantUow = Substitute.For<ITenantUnitOfWork>();
        var employeeRepo = Substitute.For<ITenantRepository<Employee, Guid>>();
        var customerUserRepo = Substitute.For<ITenantRepository<CustomerUser, Guid>>();

        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.Repository<CustomerUser>().Returns(customerUserRepo);
        customerUserRepo.Query().Returns(new List<CustomerUser>().BuildMock());

        // The filtered lookup a soft-deleted (or merged-away) Employee resolves to today - the
        // point being pinned is that nothing here reaches for IgnoreQueryFilters() to work around it.
        employeeRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var sut = new GetCurrentUserPermissionsHandler(NewUserManager(user), NewRoleManager(), tenantUow);

        var result = await sut.Handle(
            new GetCurrentUserPermissionsQuery { UserId = userId, TenantId = tenantId }, default);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    #endregion

    #region DeleteEmployeeHandler live-login guard

    [Fact]
    public async Task DeletingAnEmployeeWithALiveLogin_WithoutConfirmation_IsRefused()
    {
        var employee = new Employee
        {
            Code = "EMP-001",
            Email = "juan@example.com",
            FirstName = "Juan",
            LastName = "Dela Cruz"
        };

        var tenantUow = Substitute.For<ITenantUnitOfWork>();
        var employeeRepo = Substitute.For<ITenantRepository<Employee, Guid>>();
        tenantUow.Repository<Employee>().Returns(employeeRepo);
        employeeRepo.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var masterUow = Substitute.For<IMasterUnitOfWork>();
        var userRepo = Substitute.For<IMasterRepository<User, Guid>>();
        masterUow.Repository<User>().Returns(userRepo);

        var liveUser = new User { Id = employee.Id, FirstName = "Juan", LastName = "Dela Cruz" };
        userRepo.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(liveUser);

        var sut = new DeleteEmployeeHandler(tenantUow, masterUow);

        var result = await sut.Handle(new DeleteEmployeeCommand { Id = employee.Id }, default);

        Assert.False(result.IsSuccess);

        // The guard has to refuse before the transaction starts - not roll one back after the fact.
        await tenantUow.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<TransactionOutcome<Result>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletingAnEmployeeWhoseLoginIsAlreadyGone_NeedsNoConfirmation()
    {
        var employee = new Employee
        {
            Code = "EMP-002",
            Email = "maria@example.com",
            FirstName = "Maria",
            LastName = "Santos"
        };

        var tenantUow = Substitute.For<ITenantUnitOfWork>();
        var employeeRepo = Substitute.For<ITenantRepository<Employee, Guid>>();
        var loadRepo = Substitute.For<ITenantRepository<Load, Guid>>();
        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.Repository<Load>().Returns(loadRepo);
        employeeRepo.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        loadRepo.Query().Returns(new List<Load>().BuildMock());

        tenantUow.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<TransactionOutcome<Result>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var work = call.Arg<Func<CancellationToken, Task<TransactionOutcome<Result>>>>();
                var outcome = await work(CancellationToken.None);
                return outcome.Value;
            });

        var masterUow = Substitute.For<IMasterUnitOfWork>();
        var userRepo = Substitute.For<IMasterRepository<User, Guid>>();
        masterUow.Repository<User>().Returns(userRepo);

        // Already offboarded at the platform level - deleting the master record needs no special
        // confirmation, the same as any ordinary employee with no live login ever did.
        var goneUser = new User
        {
            Id = employee.Id,
            FirstName = "Maria",
            LastName = "Santos",
            DeletionRequestedAt = DateTime.UtcNow
        };
        userRepo.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(goneUser);

        var sut = new DeleteEmployeeHandler(tenantUow, masterUow);

        var result = await sut.Handle(new DeleteEmployeeCommand { Id = employee.Id }, default);

        Assert.True(result.IsSuccess);
    }

    #endregion

    #region MergeEmployeesHandler live-login guard

    [Fact]
    public async Task MergingAwayAnEmployeeWithALiveLogin_WithoutConfirmation_IsRefused()
    {
        var survivorId = Guid.NewGuid();
        var loserId = Guid.NewGuid();

        var tenantUow = Substitute.For<ITenantUnitOfWork>();
        var masterUow = Substitute.For<IMasterUnitOfWork>();
        var userRepo = Substitute.For<IMasterRepository<User, Guid>>();
        masterUow.Repository<User>().Returns(userRepo);

        var loserLiveUser = new User { Id = loserId, FirstName = "Loser", LastName = "Record" };
        userRepo.GetByIdAsync(loserId, Arg.Any<CancellationToken>()).Returns(loserLiveUser);

        var sut = new MergeEmployeesHandler(tenantUow, masterUow, Substitute.For<IMasterDataMergeService>());

        var result = await sut.Handle(
            new MergeEmployeesCommand
            {
                SurvivorId = survivorId,
                LoserId = loserId,
                Reason = "Duplicate record cleanup."
            }, default);

        Assert.False(result.IsSuccess);

        // Refused before the transaction ever starts - the merge must not have touched either record.
        await tenantUow.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<TransactionOutcome<Result<EmployeeDto>>>>>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
