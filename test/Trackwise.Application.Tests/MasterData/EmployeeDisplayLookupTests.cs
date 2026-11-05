using Trackwise.Application.Modules.MasterData.Queries;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Employee's branch name is resolved the same way Port's and Truck's are - a batched lookup handed
/// in, never <c>entity.Branch</c> read inside the projection loop, which lazy loading would turn into
/// a query per row. These pin the same three guarantees the pattern gives every branch-scoped entity.
/// </summary>
public class EmployeeDisplayLookupTests
{
    private readonly ITenantRepository<Employee, Guid> employeeRepo =
        Substitute.For<ITenantRepository<Employee, Guid>>();

    private readonly ITenantRepository<Branch, Guid> branchRepo =
        Substitute.For<ITenantRepository<Branch, Guid>>();

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();

    private readonly Branch branch = new() { Name = "Manila", Code = "MNL" };

    public EmployeeDisplayLookupTests()
    {
        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.Repository<Branch>().Returns(branchRepo);

        // Deliberately left with no Branch navigation loaded: the point is that the name is
        // resolved without it.
        employeeRepo.Query().Returns(new List<Employee>
        {
            new()
            {
                Code = "EMP-001",
                Email = "juan@example.com",
                FirstName = "Juan",
                LastName = "Dela Cruz",
                BranchId = branch.Id
            },
            new()
            {
                Code = "EMP-002",
                Email = "maria@example.com",
                FirstName = "Maria",
                LastName = "Santos"
            }
        }.BuildMock());

        branchRepo.Query().Returns(new List<Branch> { branch }.BuildMock());
    }

    [Fact]
    public async Task EmployeeList_ResolvesBranchNameWithoutTheNavigationProperty()
    {
        var sut = new GetEmployeesHandler(tenantUow);

        var result = await sut.Handle(new GetEmployeesQuery(), default);

        var scoped = result.Value!.Single(e => e.Code == "EMP-001");

        Assert.Equal("Manila", scoped.BranchName);
    }

    [Fact]
    public async Task EmployeeList_LooksBranchesUpOnceForTheWholePage()
    {
        var sut = new GetEmployeesHandler(tenantUow);

        await sut.Handle(new GetEmployeesQuery(), default);

        // One query for the page, not one per row.
        branchRepo.Received(1).Query();
    }

    [Fact]
    public async Task EmployeeScopedToNoBranch_LeavesTheBranchNameUnset()
    {
        var sut = new GetEmployeesHandler(tenantUow);

        var result = await sut.Handle(new GetEmployeesQuery(), default);

        var global = result.Value!.Single(e => e.Code == "EMP-002");

        Assert.Null(global.BranchName);
    }
}
