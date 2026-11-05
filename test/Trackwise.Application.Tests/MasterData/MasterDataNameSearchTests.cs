using Trackwise.Application.Modules.MasterData.Queries;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Names are free-form and Postgres LIKE is case-sensitive, so a search for "shem" used to miss
/// "Shem de Guzman". In-memory string.Contains is case-sensitive too, which makes these tests
/// fail the same way the real query did.
/// </summary>
public class MasterDataNameSearchTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();

    private readonly ITenantRepository<Employee, Guid> employeeRepo =
        Substitute.For<ITenantRepository<Employee, Guid>>();

    private readonly ITenantRepository<Helper, Guid> helperRepo =
        Substitute.For<ITenantRepository<Helper, Guid>>();

    public MasterDataNameSearchTests()
    {
        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.Repository<Helper>().Returns(helperRepo);

        employeeRepo.Query().Returns(new List<Employee>
        {
            new() { Code = "EMP-000001", Email = "shem@skybest.test", FirstName = "Shem", LastName = "de Guzman" },
            new() { Code = "EMP-000002", Email = "fely@crew.test", FirstName = "Fely", LastName = "Moreno" }
        }.BuildMock());

        helperRepo.Query().Returns(new List<Helper>
        {
            new() { Code = "HLP-000001", Name = "Gabriel Dizon" },
            new() { Code = "HLP-000002", Name = "Rodel Atadero" }
        }.BuildMock());
    }

    [Theory]
    [InlineData("shem")]
    [InlineData("Shem")]
    [InlineData("SHEM")]
    [InlineData("  de guzman ")]
    public async Task GetEmployees_NameSearchIsCaseInsensitive(string search)
    {
        var sut = new GetEmployeesHandler(tenantUow);

        var result = await sut.Handle(new GetEmployeesQuery { Search = search }, CancellationToken.None);

        var employee = Assert.Single(result.Value!);
        Assert.Equal("Shem de Guzman", employee.FullName);
    }

    [Theory]
    [InlineData("gabriel")]
    [InlineData("DIZON")]
    public async Task GetHelpers_NameSearchIsCaseInsensitive(string search)
    {
        var sut = new GetHelpersHandler(tenantUow);

        var result = await sut.Handle(new GetHelpersQuery { Search = search }, CancellationToken.None);

        var helper = Assert.Single(result.Value!);
        Assert.Equal("Gabriel Dizon", helper.Name);
    }
}
