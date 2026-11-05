using Trackwise.Application.Modules.Operations.Containers.Queries;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

/// <summary>
/// Terminal display values used to come off the <c>CurrentTerminal</c> navigation inside the
/// mapper. That was always an N+1, and once terminals became soft-deletable it also started
/// returning null for a retired port - blanking the terminal on containers still sitting at one.
/// These pin the replacement: the handler batches the lookup and hands the values in.
/// </summary>
public class TerminalDisplayLookupTests
{
    private readonly ITenantRepository<Container, Guid> containerRepo =
        Substitute.For<ITenantRepository<Container, Guid>>();

    private readonly ITenantRepository<Terminal, Guid> terminalRepo =
        Substitute.For<ITenantRepository<Terminal, Guid>>();

    private readonly ITenantRepository<ContainerType, Guid> containerTypeRepo =
        Substitute.For<ITenantRepository<ContainerType, Guid>>();

    private readonly ContainerType containerType = new()
    {
        Code = "HC40",
        Name = "40' High Cube",
        Category = ContainerTypeCategory.GeneralPurpose
    };

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();

    private readonly Terminal terminal = new()
    {
        Name = "Manila International Container Terminal",
        Code = "PHMNL",
        CountryCode = "PH",
        Type = TerminalType.SeaPort,
        Address = new Address
        {
            StreetAddress = "Eva Macapagal Super Terminal",
            CityOrMunicipality = "Manila",
            Province = "Metro Manila",
            Region = "NCR",
            Zipcode = "1018",
            Country = "PH"
        }
    };

    public TerminalDisplayLookupTests()
    {
        tenantUow.Repository<Container>().Returns(containerRepo);
        tenantUow.Repository<Terminal>().Returns(terminalRepo);
        tenantUow.Repository<ContainerType>().Returns(containerTypeRepo);

        // Deliberately left with a null CurrentTerminal navigation: the point is that the name is
        // resolved without it.
        containerRepo.Query().Returns(new List<Container>
        {
            new() { Number = "MSCU1234567", ContainerTypeId = containerType.Id, CurrentTerminalId = terminal.Id },
            new() { Number = "TCLU7654321", ContainerTypeId = containerType.Id }
        }.BuildMock());

        terminalRepo.Query().Returns(new List<Terminal> { terminal }.BuildMock());

        // The container type is batched the same way the terminal is, and for the same two
        // reasons - it is a query per row off the navigation, and the type is soft-deletable.
        containerTypeRepo.Query().Returns(new List<ContainerType> { containerType }.BuildMock());
    }

    [Fact]
    public async Task ContainerList_ResolvesTerminalNameWithoutTheNavigationProperty()
    {
        var sut = new GetContainersHandler(tenantUow);

        var result = await sut.Handle(new GetContainersQuery { Page = 1, PageSize = 50 }, default);

        var atTerminal = result.Value!.Single(c => c.Number == "MSCU1234567");

        Assert.Equal("Manila International Container Terminal", atTerminal.CurrentTerminalName);
        Assert.Equal("PHMNL", atTerminal.CurrentTerminalCode);
    }

    [Fact]
    public async Task ContainerList_LooksTerminalsUpOnceForTheWholePage()
    {
        var sut = new GetContainersHandler(tenantUow);

        await sut.Handle(new GetContainersQuery { Page = 1, PageSize = 50 }, default);

        // One query for the page, not one per row. A container list is the busiest screen that
        // shows a terminal name, so this is where the N+1 actually hurt.
        terminalRepo.Received(1).Query();
    }

    [Fact]
    public async Task ContainerWithNoTerminal_LeavesTheDisplayValuesUnset()
    {
        var sut = new GetContainersHandler(tenantUow);

        var result = await sut.Handle(new GetContainersQuery { Page = 1, PageSize = 50 }, default);

        var unassigned = result.Value!.Single(c => c.Number == "TCLU7654321");

        Assert.Null(unassigned.CurrentTerminalName);
        Assert.Null(unassigned.CurrentTerminalCode);
    }
}
