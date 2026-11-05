using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

/// <summary>
/// The truck is at one place at a time: a stop's recorded visit can't overlap another's, the crew
/// leaves one stop before arriving at the next, and a trip that has run can't be cancelled.
/// </summary>
public class TripVisitOverlapTests
{
    private const TripEventSource App = TripEventSource.DriverApp;

    private static Load NewLoad(string name, double lng, double lat) => new()
    {
        Name = name,
        Type = LoadType.RefrigeratedGoods,
        Customer = null!,
        OriginAddress = new Address { StreetAddress = "HFI Calauan farm", CityOrMunicipality = "Calauan", Province = "Laguna", Region = "", Zipcode = "", Country = "PH" },
        OriginLocation = new GeoPoint(121.3141, 14.1497),
        DestinationAddress = new Address { StreetAddress = name, CityOrMunicipality = "Quezon City", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        DestinationLocation = new GeoPoint(lng, lat),
        DeliveryCost = Money.Zero("PHP"),
        PlannedBoxes = 20
    };

    /// <summary>BKN-RFV 1446: one warehouse visit, then Bonifacio ES and Mabini ES.</summary>
    private static (Trip trip, TripStop warehouse, TripStop drop1, TripStop drop2) HmiTrip()
    {
        var trip = Trip.Create("ABC1267 - HFI", null,
            [NewLoad("BONIFACIO ES", 121.0039423, 14.6627943), NewLoad("MABINI ES", 121.008911, 14.654723)]);
        trip.Dispatch();
        var drops = trip.Stops.Where(s => s.Type == TripStopType.DropOff).OrderBy(s => s.Order).ToList();
        return (trip, trip.Stops.First(s => s.Type == TripStopType.PickUp), drops[0], drops[1]);
    }

    private static void ArriveAtWarehouse(Trip trip, TripStop warehouse, DateTime at, TripEventSource source = TripEventSource.Office)
    {
        foreach (var pickup in trip.GetSameVisitPickups(warehouse.Id))
        {
            trip.MarkStopArrived(pickup.Id, arrivedAt: at, source: source);
        }
    }

    private static void LeaveWarehouse(Trip trip, TripStop warehouse, DateTime at)
    {
        foreach (var pickup in trip.GetSameVisitPickups(warehouse.Id))
        {
            trip.MarkStopDeparted(pickup.Id, new StopDeparture(null, DepartedAt: at));
        }
    }

    private static DateTime MinutesAgo(int minutes) => DateTime.UtcNow.AddMinutes(-minutes);

    [Fact]
    public void MarkStopArrived_InsideAnotherStopsVisit_IsRefused()
    {
        var (trip, warehouse, drop1, _) = HmiTrip();
        ArriveAtWarehouse(trip, warehouse, MinutesAgo(25));
        LeaveWarehouse(trip, warehouse, MinutesAgo(13));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            trip.MarkStopArrived(drop1.Id, arrivedAt: MinutesAgo(19), source: App));

        Assert.Contains("overlaps the times recorded at the warehouse", ex.Message);
    }

    [Fact]
    public void MarkStopDeparted_SpanningAnotherStopsArrival_IsRefused()
    {
        var (trip, warehouse, drop1, drop2) = HmiTrip();
        ArriveAtWarehouse(trip, warehouse, MinutesAgo(100));
        LeaveWarehouse(trip, warehouse, MinutesAgo(90));
        trip.MarkStopArrived(drop1.Id, arrivedAt: MinutesAgo(60));
        trip.MarkStopArrived(drop2.Id, arrivedAt: MinutesAgo(40));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            trip.MarkStopDeparted(drop1.Id, LoadStatus.Delivered, departedAt: MinutesAgo(30)));

        Assert.Contains("drop 2", ex.Message);
    }

    [Fact]
    public void MarkStopArrived_OfficeBackfillingAnEarlierDrop_IsAllowed()
    {
        var (trip, warehouse, drop1, drop2) = HmiTrip();
        ArriveAtWarehouse(trip, warehouse, MinutesAgo(100));
        LeaveWarehouse(trip, warehouse, MinutesAgo(90));
        trip.MarkStopArrived(drop2.Id, arrivedAt: MinutesAgo(40));
        trip.MarkStopDeparted(drop2.Id, LoadStatus.Delivered, departedAt: MinutesAgo(30));

        trip.MarkStopArrived(drop1.Id, arrivedAt: MinutesAgo(70));
        trip.MarkStopDeparted(drop1.Id, LoadStatus.Delivered, departedAt: MinutesAgo(50));

        Assert.Equal(TripStatus.Completed, trip.Status);
    }

    [Fact]
    public void MarkStopArrived_FromTheAppBeforeLeavingTheWarehouse_IsRefused()
    {
        var (trip, warehouse, drop1, _) = HmiTrip();
        ArriveAtWarehouse(trip, warehouse, MinutesAgo(30), App);

        var ex = Assert.Throws<InvalidOperationException>(() => trip.MarkStopArrived(drop1.Id, source: App));

        Assert.Equal("Record the departure from the warehouse first.", ex.Message);
    }

    [Fact]
    public void CorrectStop_IntoAnotherStopsVisit_IsRefused()
    {
        var (trip, warehouse, drop1, _) = HmiTrip();
        ArriveAtWarehouse(trip, warehouse, MinutesAgo(100));
        LeaveWarehouse(trip, warehouse, MinutesAgo(90));
        trip.MarkStopArrived(drop1.Id, arrivedAt: MinutesAgo(60));

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectStop(drop1.Id, new StopCorrection(MinutesAgo(95)), "typo", "office"));
    }

    [Fact]
    public void Cancel_AfterADropWasReached_IsRefusedWithTheDrop()
    {
        var (trip, warehouse, drop1, _) = HmiTrip();
        ArriveAtWarehouse(trip, warehouse, MinutesAgo(100));
        LeaveWarehouse(trip, warehouse, MinutesAgo(90));
        trip.MarkStopArrived(drop1.Id, arrivedAt: MinutesAgo(60));

        var ex = Assert.Throws<InvalidOperationException>(trip.Cancel);

        Assert.StartsWith("Drop 1 is already delivered", ex.Message);
        Assert.Equal(TripStatus.InTransit, trip.Status);
    }

    [Fact]
    public void Cancel_LoadedButNoDropReached_CancelsTheLoads()
    {
        var (trip, warehouse, _, _) = HmiTrip();
        ArriveAtWarehouse(trip, warehouse, MinutesAgo(30));

        trip.Cancel();

        Assert.Equal(TripStatus.Cancelled, trip.Status);
        Assert.All(trip.Stops, s => Assert.Equal(LoadStatus.Cancelled, s.Load.Status));
    }
}
