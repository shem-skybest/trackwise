using Trackwise.Domain.Entities;
using Trackwise.Domain.Events;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class TripStatusTests
{
    private static Load NewLoad(string name) => new()
    {
        Name = name,
        Type = LoadType.GeneralFreight,
        Customer = null!,
        OriginAddress = new Address
        {
            StreetAddress = "1 Origin", CityOrMunicipality = "City", Province = "ST", Region = "",
            Zipcode = "00000", Country = "US"
        },
        OriginLocation = new GeoPoint(0, 0),
        DestinationAddress = new Address
        {
            StreetAddress = "1 Dest", CityOrMunicipality = "City", Province = "ST", Region = "",
            Zipcode = "00000", Country = "US"
        },
        DestinationLocation = new GeoPoint(0, 0),
        DeliveryCost = Money.Zero("USD")
    };

    private static (Trip trip, Load load) SingleDropTrip()
    {
        var load = NewLoad("Load 1");
        var trip = Trip.Create("Trip 1", truck: null, loads: [load]);
        return (trip, load);
    }

    [Fact]
    public void MarkStopArrived_DropOffStop_SetsTemperatureAndPicksUpLoad()
    {
        var (trip, load) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);

        trip.MarkStopArrived(pickupStop.Id, temperatureAtArrival: -18.5m);

        Assert.Equal(-18.5m, pickupStop.TemperatureAtArrival);
        Assert.Equal(LoadStatus.PickedUp, load.Status);
        Assert.Equal(TripStatus.InTransit, trip.Status);
    }

    [Fact]
    public void MarkStopDeparted_DeliveredOutcome_CompletesTripOnLastDrop()
    {
        var (trip, load) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        var dropStop = trip.Stops.Single(s => s.Type == TripStopType.DropOff);

        trip.MarkStopArrived(pickupStop.Id);
        trip.MarkStopArrived(dropStop.Id, temperatureAtArrival: -16m);
        trip.MarkStopDeparted(dropStop.Id, LoadStatus.Delivered, temperatureAtDeparture: -15.8m);

        Assert.Equal(LoadStatus.Delivered, load.Status);
        Assert.Equal(-15.8m, dropStop.TemperatureAtDeparture);
        Assert.NotNull(dropStop.DepartedAt);
        Assert.Equal(TripStatus.Completed, trip.Status);
        Assert.NotNull(trip.CompletedAt);
    }

    [Theory]
    [InlineData(LoadStatus.FoulTrip)]
    [InlineData(LoadStatus.CancelledByClient)]
    public void MarkStopDeparted_NonDeliveredOutcome_StillResolvesAndCompletesTrip(LoadStatus outcome)
    {
        // A drop that failed is still a resolved outcome - the trip must not stay stuck InTransit
        // forever just because nothing was ever marked Delivered.
        var (trip, load) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        var dropStop = trip.Stops.Single(s => s.Type == TripStopType.DropOff);

        trip.MarkStopArrived(pickupStop.Id);
        trip.MarkStopArrived(dropStop.Id);
        trip.MarkStopDeparted(dropStop.Id, outcome);

        Assert.Equal(outcome, load.Status);
        Assert.Equal(TripStatus.Completed, trip.Status);
    }

    [Fact]
    public void MarkStopDeparted_OneOfTwoDropsUnresolved_TripStaysInTransit()
    {
        var loadA = NewLoad("Load A");
        var loadB = NewLoad("Load B");
        var trip = Trip.Create("Trip 2", truck: null, loads: [loadA, loadB]);

        foreach (var stop in trip.Stops.Where(s => s.Type == TripStopType.PickUp))
        {
            trip.MarkStopArrived(stop.Id);
        }

        var dropA = trip.Stops.First(s => s.Type == TripStopType.DropOff && s.LoadId == loadA.Id);
        trip.MarkStopArrived(dropA.Id);
        trip.MarkStopDeparted(dropA.Id, LoadStatus.Delivered);

        Assert.Equal(TripStatus.InTransit, trip.Status);
    }

    [Fact]
    public void MarkStopArrived_LastDrop_TripStaysInTransitUntilItsOutcomeIsRecorded()
    {
        var (trip, _) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        var dropStop = trip.Stops.Single(s => s.Type == TripStopType.DropOff);

        trip.MarkStopArrived(pickupStop.Id);
        trip.MarkStopArrived(dropStop.Id);

        Assert.Equal(TripStatus.InTransit, trip.Status);
        Assert.Null(trip.CompletedAt);
    }

    [Fact]
    public void MarkStopDeparted_Twice_RaisesTripCompletedOnce()
    {
        var (trip, _) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        var dropStop = trip.Stops.Single(s => s.Type == TripStopType.DropOff);

        trip.MarkStopArrived(pickupStop.Id);
        trip.MarkStopArrived(dropStop.Id);
        trip.MarkStopDeparted(dropStop.Id, LoadStatus.Delivered);

        Assert.Throws<InvalidOperationException>(() => trip.MarkStopDeparted(dropStop.Id, LoadStatus.Delivered));
        Assert.Single(trip.DomainEvents.OfType<TripCompletedEvent>());
    }

    #region Pickup departure

    [Fact]
    public void MarkStopDeparted_PickupWithoutOutcome_LeavesLoadPickedUp()
    {
        var (trip, load) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);

        trip.MarkStopArrived(pickupStop.Id, temperatureAtArrival: 13m);
        trip.MarkStopDeparted(pickupStop.Id, outcome: null);

        Assert.NotNull(pickupStop.DepartedAt);
        Assert.Equal(LoadStatus.PickedUp, load.Status);
        Assert.Equal(TripStatus.InTransit, trip.Status);
    }

    [Fact]
    public void MarkStopDeparted_PickupWithOutcome_Throws()
    {
        var (trip, _) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        trip.MarkStopArrived(pickupStop.Id);

        Assert.Throws<InvalidOperationException>(() => trip.MarkStopDeparted(pickupStop.Id, LoadStatus.Delivered));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(LoadStatus.PickedUp)]
    public void MarkStopDeparted_DropOffWithoutResolvedOutcome_Throws(LoadStatus? outcome)
    {
        var (trip, _) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        var dropStop = trip.Stops.Single(s => s.Type == TripStopType.DropOff);
        trip.MarkStopArrived(pickupStop.Id);
        trip.MarkStopArrived(dropStop.Id);

        Assert.Throws<InvalidOperationException>(() => trip.MarkStopDeparted(dropStop.Id, outcome));
    }

    [Fact]
    public void MarkStopDeparted_BeforeArrival_Throws()
    {
        var (trip, _) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);

        Assert.Throws<InvalidOperationException>(() => trip.MarkStopDeparted(pickupStop.Id, outcome: null));
    }

    #endregion

    #region Reported times

    [Fact]
    public void MarkStopArrived_ReportedTime_StoresItAndWhenItWasEntered()
    {
        var (trip, load) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        var reported = DateTime.UtcNow.AddHours(-9);

        trip.MarkStopArrived(pickupStop.Id, arrivedAt: reported, recordedBy: "Trucking Subordinate");

        Assert.Equal(reported, pickupStop.ArrivedAt);
        Assert.Equal(reported, load.PickedUpAt);
        Assert.Equal("Trucking Subordinate", pickupStop.ArrivalRecordedBy);
        Assert.True(pickupStop.ArrivalRecordedAt > reported);
    }

    [Fact]
    public void MarkStopDeparted_ReportedTimes_CompletesTripAtLastDeparture()
    {
        var (trip, load) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        var dropStop = trip.Stops.Single(s => s.Type == TripStopType.DropOff);
        var start = DateTime.UtcNow.AddHours(-12);

        trip.MarkStopArrived(pickupStop.Id, arrivedAt: start);
        trip.MarkStopDeparted(pickupStop.Id, outcome: null, departedAt: start.AddHours(4));
        trip.MarkStopArrived(dropStop.Id, arrivedAt: start.AddHours(6));
        trip.MarkStopDeparted(dropStop.Id, LoadStatus.Delivered, departedAt: start.AddHours(7));

        Assert.Equal(start.AddHours(6), load.DeliveredAt);
        Assert.Equal(TripStatus.Completed, trip.Status);
        Assert.Equal(start.AddHours(7), trip.CompletedAt);
    }

    [Fact]
    public void MarkStopArrived_FutureTime_Throws()
    {
        var (trip, _) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);

        Assert.Throws<InvalidOperationException>(
            () => trip.MarkStopArrived(pickupStop.Id, arrivedAt: DateTime.UtcNow.AddHours(1)));
        Assert.Null(pickupStop.ArrivedAt);
    }

    [Fact]
    public void MarkStopDeparted_BeforeArrivalTime_Throws()
    {
        var (trip, _) = SingleDropTrip();
        var pickupStop = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        var arrived = DateTime.UtcNow.AddHours(-2);
        trip.MarkStopArrived(pickupStop.Id, arrivedAt: arrived);

        Assert.Throws<InvalidOperationException>(
            () => trip.MarkStopDeparted(pickupStop.Id, outcome: null, departedAt: arrived.AddMinutes(-1)));
    }

    #endregion

    #region Handling times, returns and garage times

    private static (Trip trip, Load load, TripStop pickup, TripStop drop) ArrivedAtDrop(DateTime start)
    {
        var (trip, load) = SingleDropTrip();
        var pickup = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        var drop = trip.Stops.Single(s => s.Type == TripStopType.DropOff);
        trip.Dispatch();
        trip.MarkStopArrived(pickup.Id, arrivedAt: start);
        trip.MarkStopDeparted(pickup.Id, new StopDeparture(null, DepartedAt: start.AddHours(1)));
        trip.MarkStopArrived(drop.Id, arrivedAt: start.AddHours(2));
        return (trip, load, pickup, drop);
    }

    [Fact]
    public void MarkStopDeparted_HandlingTimes_StoredAndOrdered()
    {
        var start = DateTime.UtcNow.AddHours(-10);
        var (trip, _, _, drop) = ArrivedAtDrop(start);

        trip.MarkStopDeparted(drop.Id, new StopDeparture(
            LoadStatus.Delivered,
            DepartedAt: start.AddHours(3),
            HandlingStartedAt: start.AddHours(2).AddMinutes(10),
            HandlingFinishedAt: start.AddHours(2).AddMinutes(40)));

        Assert.Equal(start.AddHours(2).AddMinutes(10), drop.HandlingStartedAt);
        Assert.Equal(start.AddHours(2).AddMinutes(40), drop.HandlingFinishedAt);
    }

    [Fact]
    public void MarkStopDeparted_HandlingFinishedAfterDeparture_Throws()
    {
        var start = DateTime.UtcNow.AddHours(-10);
        var (trip, _, _, drop) = ArrivedAtDrop(start);

        Assert.Throws<InvalidOperationException>(() => trip.MarkStopDeparted(drop.Id, new StopDeparture(
            LoadStatus.Delivered,
            DepartedAt: start.AddHours(3),
            HandlingFinishedAt: start.AddHours(4))));
    }

    [Fact]
    public void MarkStopDeparted_PartialReturn_RecordedOnLoad()
    {
        var start = DateTime.UtcNow.AddHours(-10);
        var (trip, load, _, drop) = ArrivedAtDrop(start);
        load.PlannedBoxes = 18;

        trip.MarkStopDeparted(drop.Id, new StopDeparture(
            LoadStatus.Delivered,
            DepartedAt: start.AddHours(3),
            ReturnedBoxes: 1,
            ReturnReason: DropReturnReason.Expired,
            OutcomeNote: "1 box expired PO"));

        Assert.Equal(LoadStatus.Delivered, load.Status);
        Assert.Equal(1, load.ReturnedBoxes);
        Assert.Equal(DropReturnReason.Expired, load.ReturnReason);
        Assert.Equal(TripStatus.Completed, trip.Status);
    }

    [Theory]
    [InlineData(1, null, LoadStatus.Delivered)]              // no reason
    [InlineData(19, DropReturnReason.Damaged, LoadStatus.Delivered)] // more than planned
    [InlineData(1, DropReturnReason.Damaged, LoadStatus.FoulTrip)]   // returns on a foul trip
    public void MarkStopDeparted_InvalidReturn_Throws(int returned, DropReturnReason? reason, LoadStatus outcome)
    {
        var start = DateTime.UtcNow.AddHours(-10);
        var (trip, load, _, drop) = ArrivedAtDrop(start);
        load.PlannedBoxes = 18;

        Assert.Throws<InvalidOperationException>(() => trip.MarkStopDeparted(drop.Id, new StopDeparture(
            outcome, DepartedAt: start.AddHours(3), ReturnedBoxes: returned, ReturnReason: reason)));
    }

    [Fact]
    public void RecordGarageTimes_AroundTheStops_Stored()
    {
        var start = DateTime.UtcNow.AddHours(-10);
        var (trip, _, _, drop) = ArrivedAtDrop(start);
        trip.MarkStopDeparted(drop.Id, new StopDeparture(LoadStatus.Delivered, DepartedAt: start.AddHours(3)));

        trip.RecordGarageTimes(start.AddHours(-2), start.AddHours(5));

        Assert.Equal(start.AddHours(-2), trip.GarageDepartedAt);
        Assert.Equal(start.AddHours(5), trip.GarageArrivedAt);
    }

    [Fact]
    public void RecordGarageTimes_DepartureAfterFirstArrival_Throws()
    {
        var start = DateTime.UtcNow.AddHours(-10);
        var (trip, _, _, _) = ArrivedAtDrop(start);

        Assert.Throws<InvalidOperationException>(() => trip.RecordGarageTimes(start.AddMinutes(30), null));
    }

    [Fact]
    public void RecordGarageTimes_DraftTrip_Throws()
    {
        var (trip, _) = SingleDropTrip();

        Assert.Throws<InvalidOperationException>(() => trip.RecordGarageTimes(DateTime.UtcNow.AddHours(-1), null));
    }

    [Fact]
    public void GetSameVisitPickups_TwoLoadsFromOneWarehouse_ReturnsBothPickups()
    {
        var trip = Trip.Create("Trip 3", truck: null, loads: [NewLoad("Drop A"), NewLoad("Drop B")]);
        var firstPickup = trip.Stops.First(s => s.Type == TripStopType.PickUp);

        var visit = trip.GetSameVisitPickups(firstPickup.Id);

        Assert.Equal(2, visit.Count);
        Assert.All(visit, s => Assert.Equal(TripStopType.PickUp, s.Type));
    }

    #endregion

    #region Crew

    private static Employee NewDriver(string first) => new()
    {
        Code = "",
        Email = $"{first.ToLowerInvariant()}@crew.test",
        FirstName = first,
        LastName = "Driver"
    };

    private static Helper NewHelper(string name) => new() { Code = "", Name = name };

    [Fact]
    public void Create_NoDriverGiven_DefaultsToTruckMainDriver()
    {
        var mainDriver = NewDriver("Wilson");
        var truck = new Truck { Number = "ABH2964", Name = "ABH2964", Type = TruckType.Reefer, MainDriver = mainDriver };

        var trip = Trip.Create("Trip", truck, [NewLoad("Drop")]);

        Assert.Same(mainDriver, trip.Driver);
    }

    [Fact]
    public void Create_DriverGiven_OverridesTruckMainDriver()
    {
        var truck = new Truck { Number = "ABC1267", Name = "ABC1267", Type = TruckType.Reefer, MainDriver = NewDriver("Salvador") };
        var onCall = NewDriver("Mark");

        var trip = Trip.Create("Trip", truck, [NewLoad("Drop")], driver: onCall);

        Assert.Same(onCall, trip.Driver);
    }

    [Fact]
    public void AssignCrew_TwoHelpers_Stored()
    {
        var (trip, _) = SingleDropTrip();
        var rayan = NewHelper("Rayan");
        var gabriel = NewHelper("Gabriel");

        trip.AssignCrew(NewDriver("Mark"), rayan, gabriel);

        Assert.Equal(rayan.Id, trip.HelperId);
        Assert.Equal(gabriel.Id, trip.SecondHelperId);
    }

    [Fact]
    public void AssignCrew_SameHelperTwice_Throws()
    {
        var (trip, _) = SingleDropTrip();
        var rayan = NewHelper("Rayan");

        Assert.Throws<InvalidOperationException>(() => trip.AssignCrew(null, rayan, rayan));
    }

    [Fact]
    public void AssignCrew_TripInTransit_Throws()
    {
        var (trip, _) = SingleDropTrip();
        trip.Dispatch();
        trip.MarkStopArrived(trip.Stops.Single(s => s.Type == TripStopType.PickUp).Id);

        Assert.Throws<InvalidOperationException>(() => trip.AssignCrew(NewDriver("Mark"), null, null));
    }

    #endregion

    #region Dispatch

    [Fact]
    public void Dispatch_DraftLoads_DispatchedWithTheTrip()
    {
        var first = NewLoad("Load 1");
        var second = NewLoad("Load 2");
        var trip = Trip.Create("Trip 1", truck: null, loads: [first, second]);

        trip.Dispatch();

        Assert.All([first, second], load =>
        {
            Assert.Equal(LoadStatus.Dispatched, load.Status);
            Assert.Equal(trip.DispatchedAt, load.DispatchedAt);
        });
    }

    [Fact]
    public void Dispatch_LoadAlreadyDispatched_KeepsItsDispatchTime()
    {
        var (trip, load) = SingleDropTrip();
        var earlier = DateTime.UtcNow.AddHours(-3);
        load.UpdateStatus(LoadStatus.Dispatched, at: earlier);

        trip.Dispatch();

        Assert.Equal(earlier, load.DispatchedAt);
    }

    [Fact]
    public void AddLoads_DispatchedTrip_DispatchesTheAddedLoad()
    {
        var (trip, _) = SingleDropTrip();
        trip.Dispatch();
        var added = NewLoad("Load 2");

        trip.AddLoads([added]);

        Assert.Equal(LoadStatus.Dispatched, added.Status);
    }

    [Fact]
    public void Dispatch_DraftLoadInvoice_StaysDraft()
    {
        var (trip, load) = SingleDropTrip();
        load.Invoice = new LoadInvoice
        {
            LoadId = load.Id,
            CustomerId = Guid.NewGuid(),
            Subtotal = Money.Zero("PHP"),
            TaxTotal = Money.Zero("PHP"),
            Total = Money.Zero("PHP"),
            Status = InvoiceStatus.Draft
        };

        trip.Dispatch();

        Assert.Equal(InvoiceStatus.Draft, load.Invoice.Status);
    }

    #endregion

    [Theory]
    [InlineData(LoadStatus.Draft, false)]
    [InlineData(LoadStatus.Dispatched, false)]
    [InlineData(LoadStatus.PickedUp, false)]
    [InlineData(LoadStatus.Delivered, true)]
    [InlineData(LoadStatus.Cancelled, true)]
    [InlineData(LoadStatus.FoulTrip, true)]
    [InlineData(LoadStatus.CancelledByClient, true)]
    public void LoadStatusMachine_IsResolved_MatchesTerminalStatuses(LoadStatus status, bool expected)
    {
        Assert.Equal(expected, LoadStatusMachine.IsResolved(status));
    }
}
