using Trackwise.Application.Abstractions.Dispatch;
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

public class DispatchTripHandlerTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Trip, Guid> tripRepo = Substitute.For<ITenantRepository<Trip, Guid>>();
    private readonly IDispatchEligibilityService eligibility = Substitute.For<IDispatchEligibilityService>();
    private readonly ITripCrewService crewService = Substitute.For<ITripCrewService>();
    private readonly DispatchTripHandler sut;

    public DispatchTripHandlerTests()
    {
        tenantUow.Repository<Trip>().Returns(tripRepo);
        eligibility.CheckAsync(default, default, default, default)
            .ReturnsForAnyArgs(EligibilityResult.Ok());
        sut = new DispatchTripHandler(tenantUow, eligibility, crewService, NullLogger<DispatchTripHandler>.Instance);
    }

    private static Load NewLoad() => new()
    {
        Name = "Drop",
        Type = LoadType.RefrigeratedGoods,
        Customer = null!,
        OriginAddress = new Address { StreetAddress = "1 A", CityOrMunicipality = "Makati", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        OriginLocation = new GeoPoint(121.02, 14.55),
        DestinationAddress = new Address { StreetAddress = "2 B", CityOrMunicipality = "Taguig", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        DestinationLocation = new GeoPoint(121.05, 14.52),
        DeliveryCost = Money.Zero("PHP")
    };

    [Fact]
    public async Task Handle_TruckWithoutMainDriver_ChecksEligibilityAgainstTheTripsDriver()
    {
        var truck = new Truck { Number = "ABE4715", Name = "ABE4715", Type = TruckType.Reefer };
        var driver = new Employee { Code = "", Email = "fely@crew.test", FirstName = "Fely", LastName = "Moreno" };
        var trip = Trip.Create("ABE4715 - ALDER", truck, [NewLoad()], driver: driver);
        tripRepo.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);

        var result = await sut.Handle(new DispatchTripCommand { TripId = trip.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        await eligibility.Received().CheckAsync(truck.Id, Arg.Any<Guid>(), driver.Id, Arg.Any<CancellationToken>());
        await eligibility.DidNotReceive().CheckAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), null, Arg.Any<CancellationToken>());
    }
}
