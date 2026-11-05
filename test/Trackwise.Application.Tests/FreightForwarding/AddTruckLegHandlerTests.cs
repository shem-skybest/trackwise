using MockQueryable;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Abstractions.Geocoding;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Application.Modules.Operations.FreightForwarding.Legs;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class AddTruckLegHandlerTests
{
    private static readonly Address PortAddress = new()
    {
        StreetAddress = "Gate 2, MICT", CityOrMunicipality = "Manila", Province = "Metro Manila",
        Region = "NCR", Zipcode = "1012", Country = "PH"
    };

    private static readonly Address WarehouseAddress = PortAddress with { StreetAddress = "12 Mercedes Ave", CityOrMunicipality = "Pasig" };

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Workfile, Guid> workfileRepo = Substitute.For<ITenantRepository<Workfile, Guid>>();
    private readonly ITenantRepository<Load, Guid> loadRepo = Substitute.For<ITenantRepository<Load, Guid>>();
    private readonly ITenantRepository<WorkfileContainer, Guid> slotRepo = Substitute.For<ITenantRepository<WorkfileContainer, Guid>>();
    private readonly ITenantRepository<ContainerType, Guid> typeRepo = Substitute.For<ITenantRepository<ContainerType, Guid>>();
    private readonly ITenantRepository<Terminal, Guid> terminalRepo = Substitute.For<ITenantRepository<Terminal, Guid>>();
    private readonly ITenantRepository<Location, Guid> siteRepo = Substitute.For<ITenantRepository<Location, Guid>>();
    private readonly ITenantRepository<Customer, Guid> customerRepo = Substitute.For<ITenantRepository<Customer, Guid>>();
    private readonly ITenantRepository<Employee, Guid> employeeRepo = Substitute.For<ITenantRepository<Employee, Guid>>();
    private readonly IWorkfileReferenceGenerator references = Substitute.For<IWorkfileReferenceGenerator>();
    private readonly IGeocodingService geocoding = Substitute.For<IGeocodingService>();
    private readonly ICurrentUserService currentUser = Substitute.For<ICurrentUserService>();

    private readonly Customer customer = new() { Name = "Northgate Foods" };
    private readonly ContainerType hc40 = new() { Code = "HC40", Name = "40' High Cube", Category = ContainerTypeCategory.GeneralPurpose };
    private readonly Terminal port = new() { Name = "MICT", Code = "PHMNL", CountryCode = "PH", Type = TerminalType.SeaPort, Address = PortAddress };
    private readonly Location warehouse;
    private readonly RegularWorkfile parent;
    private readonly List<WorkfileContainer> slots;
    private readonly List<Load> existingLoads = [];
    private readonly List<Workfile> existingFiles = [];

    private readonly AddTruckLegHandler sut;

    public AddTruckLegHandlerTests()
    {
        parent = RegularWorkfile.Create(
            "BKN-IMP-000001", FreightMode.Import, customer.Id, null, new WorkfileHeaderData { ReportCategory = ReportCategory.Psb });
        slots = parent.AddContainers(new WorkfileContainerSpec(hc40.Id), 2).ToList();
        warehouse = new Location
        {
            Code = "ZEN-PASIG", Name = "Northgate Pasig", Type = LocationType.DeliveryPoint,
            Address = WarehouseAddress, CustomerId = customer.Id, Latitude = 14.58, Longitude = 121.06
        };
        existingFiles.Add(parent);

        tenantUow.Repository<Workfile>().Returns(workfileRepo);
        tenantUow.Repository<Load>().Returns(loadRepo);
        tenantUow.Repository<WorkfileContainer>().Returns(slotRepo);
        tenantUow.Repository<ContainerType>().Returns(typeRepo);
        tenantUow.Repository<Terminal>().Returns(terminalRepo);
        tenantUow.Repository<Location>().Returns(siteRepo);
        tenantUow.Repository<Customer>().Returns(customerRepo);
        tenantUow.Repository<Employee>().Returns(employeeRepo);

        workfileRepo.GetByIdAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);
        workfileRepo.Query().Returns(_ => existingFiles.BuildMock());
        loadRepo.Query().Returns(_ => existingLoads.BuildMock());
        slotRepo.Query().Returns(_ => slots.BuildMock());
        typeRepo.Query().Returns(_ => new List<ContainerType> { hc40 }.BuildMock());
        terminalRepo.GetByIdAsync(port.Id, Arg.Any<CancellationToken>()).Returns(port);
        siteRepo.GetByIdAsync(warehouse.Id, Arg.Any<CancellationToken>()).Returns(warehouse);
        customerRepo.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        references.NextAsync(WorkfileSeries.TruckLeg, Arg.Any<CancellationToken>()).Returns("SLC-TRK0001-26-M");
        geocoding.GeocodeAddressAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<GeoPoint>.Ok(new GeoPoint(120.96, 14.60)));

        sut = new AddTruckLegHandler(tenantUow, references, geocoding, currentUser);
    }

    private AddTruckLegCommand Delivery(params Guid[] slotIds) => new()
    {
        WorkfileId = parent.Id,
        Role = TruckLegRole.Delivery,
        From = new TruckLegEnd { TerminalId = port.Id },
        To = new TruckLegEnd { SiteId = warehouse.Id },
        ContainerSlotIds = [.. slotIds]
    };

    [Fact]
    public async Task Handle_TwoContainers_OpensASlcTrkLegWithOneUninvoicedLoadPerBox()
    {
        IEnumerable<Load>? added = null;
        await loadRepo.AddRangeAsync(Arg.Do<IEnumerable<Load>>(l => added = l.ToList()), Arg.Any<CancellationToken>());

        var result = await sut.Handle(Delivery(slots[0].Id, slots[1].Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        await workfileRepo.Received(1).AddAsync(
            Arg.Is<Workfile>(w => w is TruckingWorkfile && ((TruckingWorkfile)w).ParentWorkfileId == parent.Id
                && w.ReferenceNumber == "SLC-TRK0001-26-M"),
            Arg.Any<CancellationToken>());
        Assert.NotNull(added);
        var loads = added!.ToList();
        Assert.Equal(2, loads.Count);
        Assert.All(loads, l => Assert.Null(l.Invoice));
        Assert.All(loads, l => Assert.Equal(result.Value, l.WorkfileId));
        Assert.All(loads, l => Assert.Equal(port.Id, l.OriginTerminalId));
        Assert.All(loads, l => Assert.Equal(warehouse.Id, l.DestinationSiteId));
        Assert.Equal(["SLC-TRK0001-26-M HC40 #1", "SLC-TRK0001-26-M HC40 #2"], loads.Select(l => l.Name));
        Assert.Equal([slots[0].Id, slots[1].Id], loads.Select(l => l.WorkfileContainerId!.Value));
    }

    [Fact]
    public async Task Handle_NoContainers_OpensOneLoadForLooseCargo()
    {
        IEnumerable<Load>? added = null;
        await loadRepo.AddRangeAsync(Arg.Do<IEnumerable<Load>>(l => added = l.ToList()), Arg.Any<CancellationToken>());

        var result = await sut.Handle(Delivery(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        var load = Assert.Single(added!);
        Assert.Null(load.WorkfileContainerId);
        Assert.Equal("SLC-TRK0001-26-M", load.Name);
    }

    [Fact]
    public async Task Handle_PortEnd_IsGeocodedFromItsAddress()
    {
        await sut.Handle(Delivery(slots[0].Id), CancellationToken.None);

        await geocoding.Received(1).GeocodeAddressAsync(
            "Gate 2, MICT", "Manila", "Metro Manila", "1012", "PH", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ContainerAlreadyOnALiveLegInTheSameRole_FailsNamingIt()
    {
        var earlier = TruckingWorkfile.CreateLeg("SLC-TRK0000-26-M", parent, TruckLegRole.Delivery, new WorkfileHeaderData());
        existingFiles.Add(earlier);
        existingLoads.Add(LoadOn(earlier.Id, slots[1].Id));

        var result = await sut.Handle(Delivery(slots[0].Id, slots[1].Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("HC40 #2", result.Error);
        Assert.DoesNotContain("HC40 #1", result.Error);
        await references.DidNotReceive().NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ContainerOnACancelledLeg_CanBeLeggedAgain()
    {
        var cancelled = TruckingWorkfile.CreateLeg("SLC-TRK0000-26-M", parent, TruckLegRole.Delivery, new WorkfileHeaderData());
        cancelled.Cancel("ops", "wrong port");
        existingFiles.Add(cancelled);
        existingLoads.Add(LoadOn(cancelled.Id, slots[0].Id));

        var result = await sut.Handle(Delivery(slots[0].Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
    }

    [Fact]
    public async Task Handle_SiteOfAnotherCustomer_Fails()
    {
        warehouse.CustomerId = Guid.NewGuid();

        var result = await sut.Handle(Delivery(slots[0].Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("another customer", result.Error);
    }

    [Fact]
    public async Task Handle_TriangularFile_Fails()
    {
        var triangular = RegularWorkfile.Create(
            "BKN-TRI-000001", FreightMode.Triangular, customer.Id, null, new WorkfileHeaderData { ReportCategory = ReportCategory.Psb });
        workfileRepo.GetByIdAsync(triangular.Id, Arg.Any<CancellationToken>()).Returns(triangular);

        var command = Delivery();
        command.WorkfileId = triangular.Id;
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("can't have truck legs", result.Error);
    }

    private Load LoadOn(Guid legId, Guid slotId)
    {
        var load = Load.Create(
            "earlier", LoadType.GeneralFreight, 0, PortAddress, new GeoPoint(120.96, 14.60),
            WarehouseAddress, new GeoPoint(121.06, 14.58), customer, null, null, withInvoice: false);
        load.WorkfileId = legId;
        load.WorkfileContainerId = slotId;
        return load;
    }
}
