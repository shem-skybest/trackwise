using Trackwise.Application.Abstractions.Notifications;
using Trackwise.Application.Modules.Operations.CrewPlan.Services;
using Trackwise.Application.Modules.Operations.Trips.Events;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Events;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class TripDriverPushHandlerTests
{
    private readonly IPushNotificationService push = Substitute.For<IPushNotificationService>();
    private readonly ICrewPlanService crewPlan = Substitute.For<ICrewPlanService>();
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly TripDriverPushHandler sut;

    private static readonly Customer Clearwater = new() { Name = "Clearwater Catch, Inc." };

    public TripDriverPushHandlerTests()
    {
        crewPlan.TripDay(Arg.Any<IEnumerable<Load>>()).Returns(new DateOnly(2026, 8, 29));
        sut = new TripDriverPushHandler(push, crewPlan, tenantUow, NullLogger<TripDriverPushHandler>.Instance);
    }

    private static Employee Driver(string name, string? token) =>
        new() { Code = name, Email = $"{name}@test.com", FirstName = name, LastName = "Driver", DeviceToken = token };

    private static Load NewLoad(string name) => new()
    {
        Name = name,
        Type = LoadType.GeneralFreight,
        Customer = Clearwater,
        OriginAddress = Address("Clearwater Warehouse"),
        OriginLocation = new GeoPoint(0, 0),
        DestinationAddress = Address(name),
        DestinationLocation = new GeoPoint(1, 1),
        DeliveryCost = Money.Zero("PHP")
    };

    private static Address Address(string street) => new()
    {
        StreetAddress = street, CityOrMunicipality = "Malabon", Province = "Metro Manila", Region = "",
        Zipcode = "", Country = "PH"
    };

    private Trip StoredTrip(Employee? driver, int drops = 4)
    {
        var loads = Enumerable.Range(1, drops).Select(i => NewLoad($"Drop {i}")).ToList();
        var trip = Trip.Create("BKN-RFV1321-26-M Clearwater Catch, Inc.", truck: null, loads: loads);
        trip.AssignCrew(driver, null, null);
        tenantUow.Repository<Trip>().GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);
        return trip;
    }

    private void Store(Employee employee) =>
        tenantUow.Repository<Employee>().GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

    [Fact]
    public async Task Handle_Dispatched_PushesTheTripsDriverWithDayClientAndDrops()
    {
        var salvador = Driver("Salvador", "token-salvador");
        var trip = StoredTrip(salvador);

        await sut.Handle(new TripDispatchedEvent(trip.Id), CancellationToken.None);

        await push.Received(1).SendNotificationAsync(
            "Trip dispatched",
            "Aug 29: Clearwater Catch, Inc., 4 drops",
            "token-salvador",
            Arg.Is<IReadOnlyDictionary<string, string>?>(d => d!["tripId"] == trip.Id.ToString()));
    }

    [Fact]
    public async Task Handle_DispatchedWithoutDevice_PushesNothing()
    {
        var trip = StoredTrip(Driver("Salvador", null));

        await sut.Handle(new TripDispatchedEvent(trip.Id), CancellationToken.None);

        await push.DidNotReceiveWithAnyArgs().SendNotificationAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Handle_DriverChanged_PushesBothDrivers()
    {
        var salvador = Driver("Salvador", "token-salvador");
        var wilson = Driver("Wilson", "token-wilson");
        Store(salvador);
        var trip = StoredTrip(wilson, drops: 1);

        await sut.Handle(new TripDriverChangedEvent(trip.Id, salvador.Id), CancellationToken.None);

        await push.Received(1).SendNotificationAsync(
            "Trip assigned to you", "Aug 29: Clearwater Catch, Inc., 1 drop", "token-wilson",
            Arg.Any<IReadOnlyDictionary<string, string>?>());
        await push.Received(1).SendNotificationAsync(
            "Trip moved to another driver", "Aug 29: Clearwater Catch, Inc., 1 drop", "token-salvador",
            Arg.Any<IReadOnlyDictionary<string, string>?>());
    }

    [Fact]
    public async Task Handle_Cancelled_PushesTheDriverItWasSentTo()
    {
        var salvador = Driver("Salvador", "token-salvador");
        Store(salvador);
        var trip = StoredTrip(salvador);

        await sut.Handle(new TripCancelledEvent(trip.Id, salvador.Id), CancellationToken.None);

        await push.Received(1).SendNotificationAsync(
            "Trip cancelled", Arg.Any<string>(), "token-salvador", Arg.Any<IReadOnlyDictionary<string, string>?>());
    }

    #region Domain events

    [Fact]
    public void AssignCrew_OnADispatchedTrip_RaisesDriverChanged()
    {
        var salvador = Driver("Salvador", null);
        var trip = StoredTrip(salvador);
        trip.Dispatch();
        trip.DomainEvents.Clear();

        trip.AssignCrew(Driver("Wilson", null), null, null);

        var changed = Assert.Single(trip.DomainEvents.OfType<TripDriverChangedEvent>());
        Assert.Equal(salvador.Id, changed.OldDriverId);
    }

    [Fact]
    public void AssignCrew_OnADraftTrip_RaisesNothing()
    {
        var trip = StoredTrip(Driver("Salvador", null));
        trip.DomainEvents.Clear();

        trip.AssignCrew(Driver("Wilson", null), null, null);

        Assert.Empty(trip.DomainEvents.OfType<TripDriverChangedEvent>());
    }

    [Fact]
    public void Cancel_DispatchedTrip_RaisesCancelledWithItsDriver_DraftDoesNot()
    {
        var salvador = Driver("Salvador", null);
        var dispatched = StoredTrip(salvador);
        dispatched.Dispatch();
        var draft = StoredTrip(salvador);

        dispatched.Cancel();
        draft.Cancel();

        Assert.Equal(salvador.Id, Assert.Single(dispatched.DomainEvents.OfType<TripCancelledEvent>()).DriverId);
        Assert.Empty(draft.DomainEvents.OfType<TripCancelledEvent>());
    }

    #endregion
}
