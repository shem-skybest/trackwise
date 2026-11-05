using System.Linq.Expressions;
using Trackwise.Application.Modules.Operations.CrewPlan.Services;
using Trackwise.Application.Modules.Operations.Loads.Services;
using Trackwise.Application.Modules.Operations.Trips.Commands;
using Trackwise.Application.Modules.Operations.Trips.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class UpdateTripHandlerTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Trip, Guid> tripRepo = Substitute.For<ITenantRepository<Trip, Guid>>();
    private readonly ITenantRepository<Load, Guid> loadRepo = Substitute.For<ITenantRepository<Load, Guid>>();
    private readonly ITenantRepository<TripStop, Guid> stopRepo = Substitute.For<ITenantRepository<TripStop, Guid>>();
    private readonly UpdateTripHandler sut;

    private readonly Employee driver = new() { Code = "", Email = "fely@crew.test", FirstName = "Fely", LastName = "Moreno" };

    public UpdateTripHandlerTests()
    {
        tenantUow.Repository<Trip>().Returns(tripRepo);
        tenantUow.Repository<Load>().Returns(loadRepo);
        tenantUow.Repository<TripStop>().Returns(stopRepo);
        sut = new UpdateTripHandler(
            tenantUow,
            Substitute.For<ILoadService>(),
            Substitute.For<ITripCrewService>(),
            Substitute.For<ICrewPlanService>(),
            NullLogger<UpdateTripHandler>.Instance);
    }

    private static Load NewLoad(string name) => new()
    {
        Name = name,
        Type = LoadType.RefrigeratedGoods,
        Customer = null!,
        OriginAddress = new Address { StreetAddress = "1 A", CityOrMunicipality = "Makati", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        OriginLocation = new GeoPoint(121.02, 14.55),
        DestinationAddress = new Address { StreetAddress = "2 B", CityOrMunicipality = "Muntinlupa", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        DestinationLocation = new GeoPoint(121.04, 14.41),
        DeliveryCost = Money.Zero("PHP")
    };

    [Fact]
    public async Task Handle_AttachExistingLoadToDispatchedTrip_AddsItsStopsAndRegistersThem()
    {
        var truck = new Truck { Number = "ABE4715", Name = "ABE4715", Type = TruckType.Reefer };
        var trip = Trip.Create("ABE4715 - ALDER", truck, [NewLoad("Vista Prime")], driver: driver);
        trip.Dispatch();
        tripRepo.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);

        var momentTheGrocer = NewLoad("Corner Grocer");
        loadRepo.GetListAsync(Arg.Any<Expression<Func<Load, bool>>>(), Arg.Any<CancellationToken>())
            .Returns([momentTheGrocer]);

        var result = await sut.Handle(new UpdateTripCommand
        {
            TripId = trip.Id,
            DriverId = trip.DriverId,
            AttachedLoadIds = [momentTheGrocer.Id]
        }, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        var newStops = trip.Stops.Where(s => s.LoadId == momentTheGrocer.Id).ToList();
        Assert.Equal(2, newStops.Count);
        foreach (var stop in newStops)
        {
            await stopRepo.Received(1).AddAsync(stop, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Handle_RemovingEveryDrop_IsRefused()
    {
        var truck = new Truck { Number = "ABE4715", Name = "ABE4715", Type = TruckType.Reefer };
        var only = NewLoad("Vista Prime");
        var trip = Trip.Create("ABE4715 - ALDER", truck, [only], driver: driver);
        trip.Dispatch();
        tripRepo.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);

        var result = await sut.Handle(new UpdateTripCommand
        {
            TripId = trip.Id,
            DriverId = trip.DriverId,
            DetachedLoadIds = [only.Id]
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        await tenantUow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RemovingAReachedDrop_FailsWithTheReason()
    {
        var truck = new Truck { Number = "ABE4715", Name = "ABE4715", Type = TruckType.Reefer };
        var reached = NewLoad("Vista Prime");
        var trip = Trip.Create("ABE4715 - ALDER", truck, [reached, NewLoad("Corner Grocer")], driver: driver);
        trip.Dispatch();
        var drop = trip.Stops.Single(s => s.LoadId == reached.Id && s.Type == TripStopType.DropOff);
        trip.MarkStopArrived(drop.Id, arrivedAt: DateTime.UtcNow.AddHours(-1));
        tripRepo.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);

        var result = await sut.Handle(new UpdateTripCommand
        {
            TripId = trip.Id,
            DriverId = trip.DriverId,
            DetachedLoadIds = [reached.Id]
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("recorded arrival", result.Error);
    }
}
