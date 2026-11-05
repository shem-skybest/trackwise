using Trackwise.Application.Modules.Operations.Trips.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class TripCrewServiceTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Trip, Guid> tripRepo = Substitute.For<ITenantRepository<Trip, Guid>>();
    private readonly TripCrewService sut;

    private readonly Employee driver = new() { Code = "", Email = "mark@crew.test", FirstName = "Mark", LastName = "Alvero" };
    private readonly Helper helper = new() { Code = "", Name = "Rayan Baylosis" };

    public TripCrewServiceTests()
    {
        tenantUow.Repository<Trip>().Returns(tripRepo);
        sut = new TripCrewService(tenantUow);
    }

    private static Truck NewTruck(string plate) => new() { Number = plate, Name = plate, Type = TruckType.Reefer };

    private static Load NewLoad() => new()
    {
        Name = "Drop",
        Type = LoadType.GeneralFreight,
        Customer = null!,
        OriginAddress = new Address { StreetAddress = "1 A", CityOrMunicipality = "QC", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        OriginLocation = new GeoPoint(121, 14),
        DestinationAddress = new Address { StreetAddress = "2 B", CityOrMunicipality = "QC", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        DestinationLocation = new GeoPoint(121.01, 14.01),
        DeliveryCost = Money.Zero("PHP")
    };

    private Trip DispatchedTrip(Truck truck, Employee? tripDriver, Helper? tripHelper)
    {
        var trip = Trip.Create("Trip", truck, [NewLoad()], driver: tripDriver, helper: tripHelper);
        trip.Dispatch();
        return trip;
    }

    [Fact]
    public async Task FindCrewConflict_DriverOnDispatchedTripOnAnotherTruck_ReturnsConflict()
    {
        var other = DispatchedTrip(NewTruck("ABD6775"), driver, null);
        var trip = Trip.Create("New", NewTruck("ABC1267"), [NewLoad()], driver: driver);
        tripRepo.Query().Returns(new List<Trip> { other, trip }.BuildMock());

        var conflict = await sut.FindCrewConflictAsync(trip);

        Assert.NotNull(conflict);
    }

    [Fact]
    public async Task FindCrewConflict_HelperOnDispatchedTripOnAnotherTruck_ReturnsConflict()
    {
        var other = DispatchedTrip(NewTruck("ABD6775"), null, helper);
        var trip = Trip.Create("New", NewTruck("ABC1267"), [NewLoad()], helper: helper);
        tripRepo.Query().Returns(new List<Trip> { other, trip }.BuildMock());

        var conflict = await sut.FindCrewConflictAsync(trip);

        Assert.NotNull(conflict);
        Assert.StartsWith("Rayan Baylosis is already on trip", conflict);
        Assert.Contains("on ABD6775, which is still dispatched", conflict);
    }

    [Fact]
    public async Task FindCrewConflict_SameTruckSecondTrip_NoConflict()
    {
        var truck = NewTruck("ABC1267");
        var firstTrip = DispatchedTrip(truck, driver, helper);
        var secondTrip = Trip.Create("2nd trip", truck, [NewLoad()], driver: driver, helper: helper);
        tripRepo.Query().Returns(new List<Trip> { firstTrip, secondTrip }.BuildMock());

        Assert.Null(await sut.FindCrewConflictAsync(secondTrip));
    }

    [Fact]
    public async Task FindCrewConflict_OtherTripOnlyDraft_NoConflict()
    {
        var draft = Trip.Create("Draft", NewTruck("ABD6775"), [NewLoad()], driver: driver);
        var trip = Trip.Create("New", NewTruck("ABC1267"), [NewLoad()], driver: driver);
        tripRepo.Query().Returns(new List<Trip> { draft, trip }.BuildMock());

        Assert.Null(await sut.FindCrewConflictAsync(trip));
    }
}
