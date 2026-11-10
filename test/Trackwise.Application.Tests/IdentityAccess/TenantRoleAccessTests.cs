using System.Linq.Expressions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.IdentityAccess.Roles.Services;
using Trackwise.Application.Modules.IdentityAccess.Users.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Identity.Claims;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Identity.Roles;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.IdentityAccess;

/// <summary>
/// The guardrails on changing access: nobody hands out what they don't hold, nobody edits their own
/// role or moves themselves, the Owner role is locked, and nobody demotes someone who outranks them.
/// </summary>
public class TenantRoleAccessTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ICurrentUserService currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserPermissionService userPermissions = Substitute.For<IUserPermissionService>();
    private readonly ITenantRepository<Employee, Guid> employees = Substitute.For<ITenantRepository<Employee, Guid>>();
    private readonly ITenantRepository<TenantRoleClaim, Guid> claims = Substitute.For<ITenantRepository<TenantRoleClaim, Guid>>();
    private readonly Dictionary<Guid, List<TenantRoleClaim>> claimsByRole = [];
    private readonly Guid callerId = Guid.NewGuid();
    private readonly TenantRoleAccess sut;

    public TenantRoleAccessTests()
    {
        tenantUow.GetCurrentTenant().Returns(new Tenant
        {
            Name = "Test",
            ConnectionString = "test",
            BillingEmail = "billing@test.com",
            CompanyAddress = new Address
            {
                StreetAddress = "1 Test St",
                CityOrMunicipality = "Test",
                Province = "Test",
                Region = "",
                Zipcode = "00000",
                Country = "PH"
            }
        });
        tenantUow.Repository<Employee>().Returns(employees);
        tenantUow.Repository<TenantRoleClaim>().Returns(claims);
        claims.GetListAsync(Arg.Any<Expression<Func<TenantRoleClaim, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var predicate = call.Arg<Expression<Func<TenantRoleClaim, bool>>>().Compile();
                return claimsByRole.Values.SelectMany(c => c).Where(predicate).ToList();
            });
        currentUser.GetUserId().Returns(callerId);
        sut = new TenantRoleAccess(tenantUow, currentUser, userPermissions);
    }

    private TenantRole Role(string name, params string[] permissions)
    {
        var role = new TenantRole(name) { DisplayName = name };
        claimsByRole[role.Id] = permissions
            .Select(p => new TenantRoleClaim(CustomClaimTypes.Permission, p) { RoleId = role.Id })
            .ToList();
        return role;
    }

    private void CallerHas(TenantRole? role, params string[] permissions)
    {
        userPermissions.GetPermissionsAsync(callerId, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(permissions.ToHashSet());
        employees.GetByIdAsync(callerId, Arg.Any<CancellationToken>())
            .Returns(new Employee { Code = "EMP-1", Email = "me@test.com", FirstName = "Me", LastName = "Caller", Role = role });
    }

    private static Employee Person(TenantRole? role) =>
        new() { Code = "EMP-2", Email = "p@test.com", FirstName = "Some", LastName = "One", Role = role };

    [Fact]
    public async Task CanEditRole_Owner_IsAlwaysRefused()
    {
        CallerHas(null, Permission.TenantRole.Manage);

        var result = await sut.CanEditRoleAsync(Role(TenantRoles.Owner), [], default);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanEditRole_TheCallersOwnRole_IsRefused()
    {
        var mine = Role(TenantRoles.SystemAdmin, Permission.TenantRole.Manage);
        CallerHas(mine, Permission.TenantRole.Manage, Permission.Load.View);

        var result = await sut.CanEditRoleAsync(mine, [Permission.Load.View], default);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanEditRole_ChangingAPermissionTheCallerLacks_IsRefused()
    {
        CallerHas(Role(TenantRoles.SystemAdmin), Permission.TenantRole.Manage, Permission.Load.View);

        var result = await sut.CanEditRoleAsync(Role(TenantRoles.Dispatcher), [Permission.Booking.ViewCosts], default);

        Assert.False(result.IsSuccess);
        Assert.Contains(Permission.Booking.ViewCosts, result.Error);
    }

    [Fact]
    public async Task CanEditRole_WithinTheCallersReach_IsAllowed()
    {
        CallerHas(Role(TenantRoles.Owner), Permission.TenantRole.Manage, Permission.Booking.ViewCosts);

        var result = await sut.CanEditRoleAsync(Role(TenantRoles.Dispatcher), [Permission.Booking.ViewCosts], default);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanAssign_ToOneself_IsRefused()
    {
        var mine = Role(TenantRoles.HumanResources, Permission.Employee.Manage);
        CallerHas(mine, Permission.Employee.Manage);
        var me = Person(mine);
        me.Id = callerId;

        var result = await sut.CanAssignAsync(me, Role(TenantRoles.Dispatcher), default);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanAssign_WithoutEmployeeManage_IsRefused()
    {
        CallerHas(null, Permission.Load.View, Permission.Load.Manage);

        var result = await sut.CanAssignAsync(Person(null), Role(TenantRoles.Dispatcher, Permission.Load.View), default);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanAssign_ARoleWithMorePowerThanTheCaller_IsRefused()
    {
        CallerHas(null, Permission.Employee.Manage, Permission.Load.View);

        var result = await sut.CanAssignAsync(
            Person(null), Role(TenantRoles.Manager, Permission.Load.View, Permission.Booking.ApproveCredit), default);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanAssign_MovingSomeoneWhoOutranksTheCaller_IsRefused()
    {
        CallerHas(null, Permission.Employee.Manage, Permission.Load.View);
        var owner = Person(Role(TenantRoles.Owner, Permission.Load.View, Permission.TenantRole.Manage));

        var result = await sut.CanAssignAsync(owner, Role(TenantRoles.Dispatcher, Permission.Load.View), default);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanAssign_WithinTheCallersReach_IsAllowed()
    {
        CallerHas(null, Permission.Employee.Manage, Permission.Load.View, Permission.Load.Manage);

        var result = await sut.CanAssignAsync(
            Person(Role(TenantRoles.Dispatcher, Permission.Load.View)),
            Role(TenantRoles.LiaisonOfficer, Permission.Load.View, Permission.Load.Manage),
            default);

        Assert.True(result.IsSuccess);
    }
}
