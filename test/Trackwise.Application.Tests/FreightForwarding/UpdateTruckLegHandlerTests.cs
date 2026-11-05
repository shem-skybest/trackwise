using MockQueryable;
using Trackwise.Application.Abstractions.Geocoding;
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

public class UpdateTruckLegHandlerTests
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
    private readonly ITenantRepository<TripStop, Guid> stopRepo = Substitute.For<ITenantRepository<TripStop, Guid>>();
    private readonly ITenantRepository<WorkfileContainer, Guid> slotRepo = Substitute.For<ITenantRepository<WorkfileContainer, Guid>>();
    private readonly ITenantRepository<ContainerType, Guid> typeRepo = Substitute.For<ITenantRepository<ContainerType, Guid>>();
    private readonly ITenantRepository<Terminal, Guid> terminalRepo = Substitute.For<ITenantRepository<Terminal, Guid>>();
    private readonly ITenantRepository<Location, Guid> siteRepo = Substitute.For<ITenantRepository<Location, Guid>>();
    private readonly ITenantRepository<Customer, Guid> customerRepo = Substitute.For<ITenantRepository<Customer, Guid>>();
    private readonly ITenantRepository<Employee, Guid> employeeRepo = Substitute.For<ITenantRepository<Employee, Guid>>();
    private readonly ITenantRepository<BusinessPartner, Guid> partnerRepo = Substitute.For<ITenantRepository<BusinessPartner, Guid>>();
    private readonly IGeocodingService geocoding = Substitute.For<IGeocodingService>();

    private readonly Customer customer = new() { Name = "Northgate Foods" };
    private readonly ContainerType hc40 = new() { Code = "HC40", Name = "40' High Cube", Category = ContainerTypeCategory.GeneralPurpose };
    private readonly Terminal port = new() { Name = "MICT", Code = "PHMNL", CountryCode = "PH", Type = TerminalType.SeaPort, Address = PortAddress };
    private readonly Location warehouse;
    private readonly RegularWorkfile parent;
    private readonly TruckingWorkfile leg;
    private readonly List<WorkfileContainer> slots;
    private readonly List<Load> loads = [];
    private readonly List<Workfile> files = [];
    private readonly List<TripStop> stops = [];

    private readonly UpdateTruckLegHandler sut;

    public UpdateTruckLegHandlerTests()
    {
        parent = RegularWorkfile.Create(
            "BKN-IMP-000001", FreightMode.Import, customer.Id, null, new WorkfileHeaderData { ReportCategory = ReportCategory.Psb });
        slots = parent.AddContainers(new WorkfileContainerSpec(hc40.Id), 2).ToList();
        leg = TruckingWorkfile.CreateLeg("SLC-TRK0001-26-M", parent, TruckLegRole.Delivery, new WorkfileHeaderData());
        warehouse = new Location
        {
            Code = "ZEN-PASIG", Name = "Northgate Pasig", Type = LocationType.DeliveryPoint,
            Address = WarehouseAddress, CustomerId = customer.Id, Latitude = 14.58, Longitude = 121.06
        };
        files.AddRange([parent, leg]);
        loads.Add(LoadOn(leg.Id, slots[0].Id));

        tenantUow.Repository<Workfile>().Returns(workfileRepo);
        tenantUow.Repository<Load>().Returns(loadRepo);
        tenantUow.Repository<TripStop>().Returns(stopRepo);
        tenantUow.Repository<WorkfileContainer>().Returns(slotRepo);
        tenantUow.Repository<ContainerType>().Returns(typeRepo);
        tenantUow.Repository<Terminal>().Returns(terminalRepo);
        tenantUow.Repository<Location>().Returns(siteRepo);
        tenantUow.Repository<Customer>().Returns(customerRepo);
        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.Repository<BusinessPartner>().Returns(partnerRepo);

        workfileRepo.GetByIdAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);
        workfileRepo.GetByIdAsync(leg.Id, Arg.Any<CancellationToken>()).Returns(leg);
        workfileRepo.Query().Returns(_ => files.BuildMock());
        loadRepo.Query().Returns(_ => loads.BuildMock());
        stopRepo.Query().Returns(_ => stops.BuildMock());
        slotRepo.Query().Returns(_ => slots.BuildMock());
        typeRepo.Query().Returns(_ => new List<ContainerType> { hc40 }.BuildMock());
        partnerRepo.Query().Returns(_ => new List<BusinessPartner>().BuildMock());
        terminalRepo.GetByIdAsync(port.Id, Arg.Any<CancellationToken>()).Returns(port);
        siteRepo.GetByIdAsync(warehouse.Id, Arg.Any<CancellationToken>()).Returns(warehouse);
        customerRepo.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        geocoding.GeocodeAddressAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<GeoPoint>.Ok(new GeoPoint(120.96, 14.60)));

        sut = new UpdateTruckLegHandler(tenantUow, geocoding);
    }

    private UpdateTruckLegCommand Edit(TruckLegRole role, TruckLegEnd from, TruckLegEnd to, params Guid[] slotIds) => new()
    {
        WorkfileId = parent.Id,
        LegId = leg.Id,
        Role = role,
        From = from,
        To = to,
        ContainerSlotIds = [.. slotIds]
    };

    private UpdateTruckLegCommand SameEnds(params Guid[] slotIds) => Edit(
        TruckLegRole.Delivery, new TruckLegEnd { TerminalId = port.Id }, new TruckLegEnd { SiteId = warehouse.Id }, slotIds);

    [Fact]
    public async Task Handle_SwappedBox_RemovesTheOldLoadAndAddsOneForTheNewBox()
    {
        var oldLoad = loads[0];
        List<Load>? added = null;
        await loadRepo.AddRangeAsync(Arg.Do<IEnumerable<Load>>(l => added = l.ToList()), Arg.Any<CancellationToken>());

        var result = await sut.Handle(SameEnds(slots[1].Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        loadRepo.Received(1).Delete(oldLoad);
        var load = Assert.Single(added!);
        Assert.Equal(slots[1].Id, load.WorkfileContainerId);
        Assert.Equal("SLC-TRK0001-26-M HC40 #2", load.Name);
        Assert.Null(load.Invoice);
    }

    [Fact]
    public async Task Handle_NewEndsAndRole_AreSetOnTheLegAndItsLoads()
    {
        var result = await sut.Handle(
            Edit(TruckLegRole.Pickup, new TruckLegEnd { SiteId = warehouse.Id }, new TruckLegEnd { TerminalId = port.Id }, slots[0].Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(TruckLegRole.Pickup, leg.LegRole);
        Assert.Equal(warehouse.Id, loads[0].OriginSiteId);
        Assert.Null(loads[0].OriginTerminalId);
        Assert.Equal(port.Id, loads[0].DestinationTerminalId);
        loadRepo.DidNotReceive().Delete(Arg.Any<Load>());
        await tenantUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LoadAlreadyDispatched_FailsAndChangesNothing()
    {
        loads[0].Dispatch();

        var result = await sut.Handle(SameEnds(slots[1].Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("already with dispatch", result.Error);
        await tenantUow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BoxOnAnotherLiveLegInTheSameRole_FailsNamingIt()
    {
        var other = TruckingWorkfile.CreateLeg("SLC-TRK0002-26-M", parent, TruckLegRole.Delivery, new WorkfileHeaderData());
        files.Add(other);
        loads.Add(LoadOn(other.Id, slots[1].Id));

        var result = await sut.Handle(SameEnds(slots[0].Id, slots[1].Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("HC40 #2", result.Error);
    }

    [Fact]
    public async Task Handle_KeepingItsOwnBox_IsNotABoxOnAnotherLeg()
    {
        var result = await sut.Handle(SameEnds(slots[0].Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
    }

    [Fact]
    public async Task Handle_LegOfAnotherWorkfile_Fails()
    {
        var command = SameEnds(slots[0].Id);
        command.WorkfileId = Guid.NewGuid();

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Could not find truck leg", result.Error);
    }

    private Load LoadOn(Guid legId, Guid slotId)
    {
        var load = Load.Create(
            "SLC-TRK0001-26-M HC40 #1", LoadType.GeneralFreight, 0, PortAddress, new GeoPoint(120.96, 14.60),
            WarehouseAddress, new GeoPoint(121.06, 14.58), customer, null, null, withInvoice: false);
        load.WorkfileId = legId;
        load.WorkfileContainerId = slotId;
        load.OriginTerminalId = port.Id;
        load.DestinationSiteId = warehouse.Id;
        return load;
    }
}
