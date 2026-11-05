using Trackwise.Application.Modules.Operations.Loads;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Identity.Claims;
using Trackwise.Shared.Identity.Policies;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class DriverLoadAccessTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly List<TripStop> stops = [];
    private readonly List<TripRescue> rescues = [];
    private readonly List<Load> loads = [];
    private readonly List<Employee> employees = [];

    private static readonly Employee Salvador = NewDriver("Salvador");
    private static readonly Employee Wilson = NewDriver("Wilson");

    public DriverLoadAccessTests()
    {
        tenantUow.Repository<TripStop>().Query().Returns(_ => stops.BuildMock());
        tenantUow.Repository<TripRescue>().Query().Returns(_ => rescues.BuildMock());
        tenantUow.Repository<Load>().Query().Returns(_ => loads.BuildMock());
        tenantUow.Repository<Employee>().Query().Returns(_ => employees.BuildMock());
    }

    private static Employee NewDriver(string name) =>
        new() { Code = name, Email = $"{name}@test.com", FirstName = name, LastName = "Driver" };

    private static Load NewLoad(string name, int drop = 1) => new()
    {
        Name = name,
        Type = LoadType.GeneralFreight,
        Customer = null!,
        OriginAddress = Address("Warehouse"),
        OriginLocation = new GeoPoint(0, 0),
        DestinationAddress = Address(name),
        DestinationLocation = new GeoPoint(drop, drop),
        DeliveryCost = Money.Zero("PHP")
    };

    private static Address Address(string street) => new()
    {
        StreetAddress = street,
        CityOrMunicipality = "Malabon",
        Province = "Metro Manila",
        Region = "",
        Zipcode = "",
        Country = "PH"
    };

    private Trip AddTrip(Employee driver, params Load[] tripLoads)
    {
        var trip = Trip.Create("Clearwater Catch", truck: null, loads: tripLoads.ToList());
        trip.AssignCrew(driver, null, null);
        stops.AddRange(trip.Stops);
        return trip;
    }

    private Task<bool> CanActOn(Employee caller, Load load) =>
        DriverLoadAccess.CanActOnAsync(tenantUow, caller.Id, load.Id, CancellationToken.None);

    [Fact]
    public async Task CanActOn_TripDriver_Allowed()
    {
        var load = NewLoad("NF Sta. Rosa");
        AddTrip(Salvador, load);

        Assert.True(await CanActOn(Salvador, load));
    }

    [Fact]
    public async Task CanActOn_AnotherDriver_Refused()
    {
        var load = NewLoad("NF Sta. Rosa");
        AddTrip(Salvador, load);

        Assert.False(await CanActOn(Wilson, load));
    }

    [Fact]
    public async Task CanActOn_DriverOfACancelledTrip_Refused()
    {
        var load = NewLoad("NF Sta. Rosa");
        var trip = AddTrip(Salvador, load);
        trip.Cancel();

        Assert.False(await CanActOn(Salvador, load));
    }

    [Fact]
    public async Task CanActOn_RescueDriverOfTheDrop_Allowed()
    {
        var day = DateTime.UtcNow.Date.AddDays(-1);
        var first = NewLoad("Drop 1", 1);
        var second = NewLoad("Drop 2", 2);
        var trip = AddTrip(Salvador, first, second);
        trip.Dispatch();
        trip.RecordGarageTimes(day.AddHours(4), null);
        foreach (var pickup in trip.Stops.Where(s => s.Type == TripStopType.PickUp))
        {
            trip.MarkStopArrived(pickup.Id, arrivedAt: day.AddHours(5));
            trip.MarkStopDeparted(pickup.Id, new StopDeparture(null, DepartedAt: day.AddHours(6)));
        }

        var secondDrop = trip.Stops.Single(s => s.LoadId == second.Id && s.Type == TripStopType.DropOff);
        var rescueTruck = new Truck { Number = "ABF3727", Name = "ABF3727", Type = TruckType.FreightTruck };
        rescues.Add(trip.RecordRescue(
            new RescueSpec(rescueTruck, Wilson, null, null, [secondDrop.Id], day.AddHours(9), "Breakdown"),
            "office"));

        Assert.True(await CanActOn(Wilson, second));
        Assert.False(await CanActOn(Wilson, first));
    }

    [Fact]
    public async Task CanActOn_LoadOnNoTrip_TrucksMainDriverAllowed()
    {
        var truck = new Truck { Number = "ABC1267", Name = "ABC1267", Type = TruckType.FreightTruck, MainDriverId = Salvador.Id };
        var load = NewLoad("Legacy load");
        load.AssignedTruck = truck;
        load.AssignedTruckId = truck.Id;
        loads.Add(load);

        Assert.True(await CanActOn(Salvador, load));
        Assert.False(await CanActOn(Wilson, load));
    }

    [Fact]
    public async Task CanActOn_OfficeWithLoadManage_Allowed()
    {
        var load = NewLoad("NF Sta. Rosa");
        AddTrip(Salvador, load);
        var dispatcher = NewDriver("Julie");
        var role = new TenantRole("dispatcher");
        role.Claims.Add(new TenantRoleClaim(CustomClaimTypes.Permission, Permission.Load.Manage));
        dispatcher.Role = role;
        employees.Add(dispatcher);

        Assert.True(await CanActOn(dispatcher, load));
    }

    [Fact]
    public async Task CanActOn_NoCaller_Refused()
    {
        var load = NewLoad("NF Sta. Rosa");
        AddTrip(Salvador, load);

        Assert.False(await DriverLoadAccess.CanActOnAsync(tenantUow, null, load.Id, CancellationToken.None));
    }
}
