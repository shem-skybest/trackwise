using Trackwise.Infrastructure.AI.Tools.Intermodal;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Infrastructure.AI.Tools;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Infrastructure.AI.Tests.Tools.Intermodal;

public class GetContainerStatusToolTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Container, Guid> containerRepo =
        Substitute.For<ITenantRepository<Container, Guid>>();
    private readonly ITenantRepository<Load, Guid> loadRepo =
        Substitute.For<ITenantRepository<Load, Guid>>();
    private readonly ITenantRepository<ContainerType, Guid> containerTypeRepo =
        Substitute.For<ITenantRepository<ContainerType, Guid>>();
    private readonly ITenantRepository<Workfile, Guid> workfileRepo =
        Substitute.For<ITenantRepository<Workfile, Guid>>();
    private readonly ITenantRepository<WorkfileContainer, Guid> workfileContainerRepo =
        Substitute.For<ITenantRepository<WorkfileContainer, Guid>>();
    private readonly GetContainerStatusTool sut;

    private readonly ContainerType containerType = new()
    {
        Code = "HC40",
        Name = "40' High Cube",
        IsoSizeTypeCode = "45G1",
        Category = ContainerTypeCategory.GeneralPurpose
    };

    public GetContainerStatusToolTests()
    {
        tenantUow.Repository<Container>().Returns(containerRepo);
        tenantUow.Repository<Load>().Returns(loadRepo);
        tenantUow.Repository<ContainerType>().Returns(containerTypeRepo);

        // Read past the soft-delete filter rather than off the navigation, so the tool still names
        // the box on a container whose type a steward retired.
        containerTypeRepo.Query().Returns(new List<ContainerType> { containerType }.BuildMock());

        tenantUow.Repository<Workfile>().Returns(workfileRepo);
        tenantUow.Repository<WorkfileContainer>().Returns(workfileContainerRepo);
        workfileRepo.Query().Returns(new List<Workfile>().BuildMock());
        workfileContainerRepo.Query().Returns(new List<WorkfileContainer>().BuildMock());

        sut = new GetContainerStatusTool(tenantUow);
    }

    [Fact]
    public async Task Execute_NoIdentifier_ReturnsErrorNamingBothParams()
    {
        var result = await sut.ExecuteAsync(new JsonObject(), CancellationToken.None);

        var error = JsonDocument.Parse(result).RootElement.GetProperty("error").GetString();
        Assert.Contains("container_number", error);
        Assert.Contains("container_id", error);
    }

    [Fact]
    public async Task Execute_UnknownNumber_ReturnsError()
    {
        containerRepo
            .GetAsync(Arg.Any<Expression<Func<Container, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((Container?)null);

        var input = new JsonObject { ["container_number"] = "MSCU9999999" };

        var result = await sut.ExecuteAsync(input, CancellationToken.None);

        var error = JsonDocument.Parse(result).RootElement.GetProperty("error").GetString();
        Assert.Contains("MSCU9999999", error);
    }

    [Fact]
    public async Task Execute_ByNumber_ShapesResponseWithTerminalAndLinkedLoad()
    {
        var terminal = new Terminal
        {
            Name = "Los Angeles",
            Code = "USLAX",
            CountryCode = "US",
            Type = TerminalType.SeaPort,
            Address = new Address
            {
                StreetAddress = "425 S Palos Verdes St",
                CityOrMunicipality = "San Pedro",
                Province = "California",
                Region = "CA",
                Zipcode = "90731",
                Country = "United States"
            }
        };

        var container = new Container
        {
            Number = "MSCU1234567",
            ContainerTypeId = containerType.Id,
            IsLaden = true,
            CurrentTerminal = terminal
        };
        container.MarkAtPort(terminal);

        var workfile = RegularWorkfile.Create(
            "SLC-IMP0001-26-M", FreightMode.Import, Guid.NewGuid(), null,
            new WorkfileHeaderData { BillOfLadingNumber = "BL-777", ReportCategory = ReportCategory.Psb });
        var slots = workfile.AddContainers(new WorkfileContainerSpec(
            containerType.Id, container.Id, SealNumber: "SL-0099", GrossWeightKg: 18500m));
        workfileRepo.Query().Returns(new List<Workfile> { workfile }.BuildMock());
        workfileContainerRepo.Query().Returns(slots.ToList().BuildMock());

        containerRepo
            .GetAsync(Arg.Any<Expression<Func<Container, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(container);
        loadRepo
            .GetAsync(Arg.Any<Expression<Func<Load, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(CreateLoad("LA to Phoenix"));

        var input = new JsonObject { ["container_number"] = "mscu1234567" };

        var result = await sut.ExecuteAsync(input, CancellationToken.None);
        var root = JsonDocument.Parse(result).RootElement;

        Assert.Equal("MSCU1234567", root.GetProperty("number").GetString());
        Assert.Equal("HC40", root.GetProperty("container_type").GetString());
        Assert.Equal("40' High Cube", root.GetProperty("container_type_description").GetString());
        Assert.Equal("45G1", root.GetProperty("iso_size_type_code").GetString());
        Assert.Equal("AtPort", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("is_laden").GetBoolean());
        Assert.Equal("SL-0099", root.GetProperty("seal_number").GetString());
        Assert.Equal("BL-777", root.GetProperty("bill_of_lading_number").GetString());
        Assert.Equal("SLC-IMP0001-26-M", root.GetProperty("latest_workfile").GetString());

        var terminalJson = root.GetProperty("current_terminal");
        Assert.Equal("USLAX", terminalJson.GetProperty("code").GetString());
        Assert.Equal("SeaPort", terminalJson.GetProperty("type").GetString());
        // Record default ToString would leak "Address { Line1 = ... }" - assert the flat form.
        Assert.Contains("San Pedro", terminalJson.GetProperty("address").GetString());
        Assert.DoesNotContain("Line1", terminalJson.GetProperty("address").GetString());

        Assert.Equal("LA to Phoenix", root.GetProperty("linked_load").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Execute_ByIdWithNoTerminalOrLoad_EmitsNulls()
    {
        var containerId = Guid.NewGuid();
        containerRepo.GetByIdAsync(containerId, Arg.Any<CancellationToken>())
            .Returns(new Container { Number = "TCLU7654321", ContainerTypeId = containerType.Id });
        loadRepo
            .GetAsync(Arg.Any<Expression<Func<Load, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((Load?)null);

        var input = new JsonObject { ["container_id"] = containerId.ToString() };

        var result = await sut.ExecuteAsync(input, CancellationToken.None);
        var root = JsonDocument.Parse(result).RootElement;

        Assert.Equal("TCLU7654321", root.GetProperty("number").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("current_terminal").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("linked_load").ValueKind);
    }

    private static Load CreateLoad(string name)
    {
        return new Load
        {
            Name = name,
            Type = LoadType.IntermodalContainer,
            Customer = null!,
            OriginAddress = new Address
            {
                StreetAddress = "1 Origin", CityOrMunicipality = "City", Province = "State", Region = "ST", Zipcode = "00000", Country = "United States"
            },
            OriginLocation = new GeoPoint(0, 0),
            DestinationAddress = new Address
            {
                StreetAddress = "1 Dest", CityOrMunicipality = "City", Province = "State", Region = "ST", Zipcode = "00000", Country = "United States"
            },
            DestinationLocation = new GeoPoint(0, 0),
            DeliveryCost = Money.Zero("USD")
        };
    }

}
