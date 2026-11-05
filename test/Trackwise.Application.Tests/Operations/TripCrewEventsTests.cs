using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

/// <summary>
/// The crew records trip events from the Driver App and the office from the crew report; both
/// write the same fields once, the crew can set a time back a little and take back their own last
/// tap for a few minutes.
/// </summary>
public class TripCrewEventsTests
{
    private const TripEventSource App = TripEventSource.DriverApp;

    private static Load NewLoad(string name) => new()
    {
        Name = name,
        Type = LoadType.RefrigeratedGoods,
        Customer = null!,
        OriginAddress = new Address { StreetAddress = "JM WHSE", CityOrMunicipality = "Makati", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        OriginLocation = new GeoPoint(121.02, 14.55),
        DestinationAddress = new Address { StreetAddress = name, CityOrMunicipality = "Pasay", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        DestinationLocation = new GeoPoint(121.0, 14.52),
        DeliveryCost = Money.Zero("PHP"),
        PlannedBoxes = 20
    };

    private static (Trip trip, TripStop pickup, TripStop drop) DispatchedTrip()
    {
        var trip = Trip.Create("ABD6775 - ALDER", null, [NewLoad("GRAND BAY RESORT")]);
        trip.Dispatch();
        return (trip,
            trip.Stops.Single(s => s.Type == TripStopType.PickUp),
            trip.Stops.Single(s => s.Type == TripStopType.DropOff));
    }

    private static DateTime MinutesAgo(int minutes) => DateTime.UtcNow.AddMinutes(-minutes);

    #region Recording

    [Fact]
    public void MarkStopArrived_FromTheApp_KeepsTheSource()
    {
        var (trip, pickup, _) = DispatchedTrip();

        trip.MarkStopArrived(pickup.Id, -10m, MinutesAgo(5), "wilson", App);

        Assert.Equal(App, pickup.ArrivalSource);
        Assert.Equal(TripStatus.InTransit, trip.Status);
        Assert.Equal(LoadStatus.PickedUp, pickup.Load.Status);
    }

    [Fact]
    public void MarkStopArrived_FromTheAppMoreThanTwoHoursBack_IsTheOfficesToEnter()
    {
        var (trip, pickup, _) = DispatchedTrip();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            trip.MarkStopArrived(pickup.Id, arrivedAt: MinutesAgo(130), source: App));

        Assert.Contains("entered by the office", ex.Message);
        trip.MarkStopArrived(pickup.Id, arrivedAt: MinutesAgo(130));
        Assert.Equal(TripEventSource.Office, pickup.ArrivalSource);
    }

    [Fact]
    public void MarkHandling_RecordedBeforeTheDeparture_IsKeptByIt()
    {
        var (trip, _, drop) = DispatchedTrip();
        trip.MarkStopArrived(drop.Id, arrivedAt: MinutesAgo(60), source: App);
        trip.MarkHandlingStarted(drop.Id, MinutesAgo(50), App);
        trip.MarkHandlingFinished(drop.Id, MinutesAgo(30), App);

        trip.MarkStopDeparted(
            drop.Id,
            new StopDeparture(LoadStatus.Delivered, DepartedAt: MinutesAgo(20),
                HandlingStartedAt: MinutesAgo(45), HandlingFinishedAt: MinutesAgo(25)));

        Assert.Equal(MinutesAgo(50), drop.HandlingStartedAt!.Value, TimeSpan.FromSeconds(5));
        Assert.Equal(MinutesAgo(30), drop.HandlingFinishedAt!.Value, TimeSpan.FromSeconds(5));
        Assert.Equal(App, drop.HandlingStartedSource);
        Assert.Equal(TripEventSource.Office, drop.DepartureSource);
    }

    [Fact]
    public void MarkHandlingStarted_BeforeTheArrival_IsRefused()
    {
        var (trip, _, drop) = DispatchedTrip();

        Assert.Throws<InvalidOperationException>(() => trip.MarkHandlingStarted(drop.Id, source: App));
    }

    [Fact]
    public void MarkHandlingFinished_EarlierThanTheStart_IsRefused()
    {
        var (trip, _, drop) = DispatchedTrip();
        trip.MarkStopArrived(drop.Id, arrivedAt: MinutesAgo(60), source: App);
        trip.MarkHandlingStarted(drop.Id, MinutesAgo(40), App);

        Assert.Throws<InvalidOperationException>(() => trip.MarkHandlingFinished(drop.Id, MinutesAgo(50), App));
    }

    [Fact]
    public void MarkGarageDeparted_Twice_IsRefused()
    {
        var (trip, _, _) = DispatchedTrip();
        trip.MarkGarageDeparted(MinutesAgo(90), App);

        Assert.Equal(App, trip.GarageDepartureSource);
        Assert.Throws<InvalidOperationException>(() => trip.MarkGarageDeparted(source: App));
    }

    #endregion

    #region Undo

    [Fact]
    public void UndoCrewEvent_TheLastArrival_PutsTheLoadAndTripBack()
    {
        var (trip, pickup, _) = DispatchedTrip();
        trip.MarkStopArrived(pickup.Id, -10m, MinutesAgo(5), "wilson", App);

        trip.UndoCrewEvent(TripEventKind.Arrived, pickup.Id);

        Assert.Null(pickup.ArrivedAt);
        Assert.Null(pickup.TemperatureAtArrival);
        Assert.Equal(LoadStatus.Dispatched, pickup.Load.Status);
        Assert.Null(pickup.Load.PickedUpAt);
        Assert.Equal(TripStatus.Dispatched, trip.Status);
    }

    [Fact]
    public void UndoCrewEvent_OnAFinishedTrip_IsRefused()
    {
        var (trip, pickup, drop) = DispatchedTrip();
        trip.MarkStopArrived(pickup.Id, arrivedAt: MinutesAgo(90), source: App);
        trip.MarkStopDeparted(pickup.Id, new StopDeparture(null, DepartedAt: MinutesAgo(60)), "wilson", App);
        trip.MarkStopArrived(drop.Id, arrivedAt: MinutesAgo(30), source: App);
        trip.MarkStopDeparted(drop.Id,
            new StopDeparture(LoadStatus.Delivered, DepartedAt: MinutesAgo(1), ReturnedBoxes: 5, ReturnReason: DropReturnReason.Other),
            "wilson", App);
        Assert.Equal(TripStatus.Completed, trip.Status);

        var ex = Assert.Throws<InvalidOperationException>(() => trip.UndoCrewEvent(TripEventKind.Departed, drop.Id));

        Assert.Contains("finished", ex.Message);
    }

    [Fact]
    public void UndoCrewEvent_ADropDeparture_ReadsDeliveredAgain()
    {
        var trip = Trip.Create("ABD6775 - ALDER", null, [NewLoad("GRAND BAY RESORT"), NewLoad("HARBOR HOTEL")]);
        trip.Dispatch();
        var drop = trip.Stops.First(s => s.Type == TripStopType.DropOff);
        trip.MarkStopArrived(drop.Id, arrivedAt: MinutesAgo(30), source: App);
        trip.MarkStopDeparted(drop.Id,
            new StopDeparture(LoadStatus.Delivered, DepartedAt: MinutesAgo(1), ReturnedBoxes: 5, ReturnReason: DropReturnReason.Other),
            "wilson", App);

        trip.UndoCrewEvent(TripEventKind.Departed, drop.Id);

        Assert.Null(drop.DepartedAt);
        Assert.Equal(LoadStatus.Delivered, drop.Load.Status);
        Assert.Null(drop.Load.ReturnedBoxes);
        Assert.Null(drop.Load.ReturnReason);
    }

    [Fact]
    public void UndoCrewEvent_WithSomethingRecordedAfter_IsRefused()
    {
        var (trip, _, drop) = DispatchedTrip();
        trip.MarkStopArrived(drop.Id, arrivedAt: MinutesAgo(10), source: App);
        Thread.Sleep(5);
        trip.MarkHandlingStarted(drop.Id, MinutesAgo(5), App);

        var ex = Assert.Throws<InvalidOperationException>(() => trip.UndoCrewEvent(TripEventKind.Arrived, drop.Id));

        Assert.Contains("recorded after", ex.Message);
        trip.UndoCrewEvent(TripEventKind.HandlingStarted, drop.Id);
        Assert.Null(drop.HandlingStartedAt);
    }

    [Fact]
    public void UndoCrewEvent_OnTheOfficesEntry_IsRefused()
    {
        var (trip, pickup, _) = DispatchedTrip();
        trip.MarkStopArrived(pickup.Id, arrivedAt: MinutesAgo(5), recordedBy: "gani");

        var ex = Assert.Throws<InvalidOperationException>(() => trip.UndoCrewEvent(TripEventKind.Arrived, pickup.Id));

        Assert.Contains("office", ex.Message);
    }

    [Fact]
    public void UndoCrewEvent_AWarehouseArrival_TakesBackTheWholeVisit()
    {
        var trip = Trip.Create("ABD6775 - ALDER", null, [NewLoad("GRAND BAY RESORT"), NewLoad("HARBOR HOTEL")]);
        trip.Dispatch();
        var pickups = trip.Stops.Where(s => s.Type == TripStopType.PickUp).ToList();
        foreach (var p in pickups)
        {
            trip.MarkStopArrived(p.Id, arrivedAt: MinutesAgo(5), source: App);
        }

        trip.UndoCrewEvent(TripEventKind.Arrived, pickups[0].Id);

        Assert.All(pickups, p => Assert.Null(p.ArrivedAt));
    }

    [Fact]
    public void UndoCrewEvent_AGarageDeparture_ClearsIt()
    {
        var (trip, _, _) = DispatchedTrip();
        trip.MarkGarageDeparted(MinutesAgo(3), App);

        trip.UndoCrewEvent(TripEventKind.GarageDeparted, null);

        Assert.Null(trip.GarageDepartedAt);
        Assert.Null(trip.GarageDepartureSource);
    }

    #endregion
}
