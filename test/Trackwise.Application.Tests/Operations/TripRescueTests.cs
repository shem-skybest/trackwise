using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

/// <summary>
/// Modelled on ABG3198, 11 Apr 2026: a four-drop Alder trip whose windshield broke after drop 2,
/// with ABF3727 finishing drops 3 and 4.
/// </summary>
public class TripRescueTests
{
    private static readonly DateTime Start =
        new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc)
            .AddDays(-1);

    private static readonly Truck OwnTruck = new() { Number = "ABG3198", Name = "ABG3198", Type = TruckType.FreightTruck };
    private static readonly Truck RescueTruck = new() { Number = "ABF3727", Name = "ABF3727", Type = TruckType.FreightTruck };

    private static readonly Employee RescueDriver =
        new() { Code = "RESCUE", Email = "rescue@test.com", FirstName = "Rescue", LastName = "Driver" };

    private static readonly Helper Helper = new() { Code = "", Name = "Rayan Baylosis" };

    private static Load NewLoad(string name, int dropIndex) => new()
    {
        Name = name,
        Type = LoadType.GeneralFreight,
        Customer = null!,
        OriginAddress = Address("Warehouse"),
        OriginLocation = new GeoPoint(0, 0),
        DestinationAddress = Address(name),
        DestinationLocation = new GeoPoint(dropIndex, dropIndex),
        DeliveryCost = Money.Zero("PHP"),
        PlannedBoxes = 10
    };

    private static Address Address(string street) => new()
    {
        StreetAddress = street, CityOrMunicipality = "Quezon City", Province = "Metro Manila", Region = "",
        Zipcode = "", Country = "PH"
    };

    /// <summary>Garage 04:00, warehouse 04:30-07:00, drops 1 and 2 delivered by 08:15; drops 3 and 4 unvisited.</summary>
    private static (Trip Trip, IReadOnlyList<TripStop> Drops) TripWithTwoDropsLeft()
    {
        var loads = Enumerable.Range(1, 4).Select(i => NewLoad($"Drop {i}", i)).ToList();
        var trip = Trip.Create("Alder QC", OwnTruck, loads);
        trip.Dispatch();
        trip.RecordGarageTimes(Start.AddHours(4), null);

        foreach (var pickup in trip.Stops.Where(s => s.Type == TripStopType.PickUp))
        {
            trip.MarkStopArrived(pickup.Id, arrivedAt: Start.AddHours(4.5));
            trip.MarkStopDeparted(pickup.Id, new StopDeparture(null, DepartedAt: Start.AddHours(7)));
        }

        var drops = trip.Stops.Where(s => s.Type == TripStopType.DropOff).OrderBy(s => s.Order).ToList();
        for (var i = 0; i < 2; i++)
        {
            trip.MarkStopArrived(drops[i].Id, arrivedAt: Start.AddHours(7.5 + i * 0.25));
            trip.MarkStopDeparted(drops[i].Id, new StopDeparture(LoadStatus.Delivered, DepartedAt: Start.AddHours(7.6 + i * 0.25)));
        }

        return (trip, drops);
    }

    private static RescueSpec Spec(IEnumerable<TripStop> stops, DateTime? at = null, Truck? truck = null) => new(
        truck ?? RescueTruck,
        RescueDriver,
        Helper,
        null,
        stops.Select(s => s.Id).ToList(),
        at ?? Start.AddHours(13),
        "Broken windshield after drop 2",
        "Brgy. Bagumbong North, Caloocan");

    [Fact]
    public void RecordRescue_TakesOverTheDrops_TripKeepsItsTruck()
    {
        var (trip, drops) = TripWithTwoDropsLeft();

        var rescue = trip.RecordRescue(Spec(drops.Skip(2)), "office");

        Assert.Equal(RescueTruck.Id, rescue.TruckId);
        Assert.Equal(RescueDriver.Id, rescue.DriverId);
        Assert.Equal(OwnTruck.Id, trip.TruckId);
        Assert.All(drops.Skip(2), d => Assert.Equal(rescue.Id, d.RescueId));
        Assert.All(drops.Take(2), d => Assert.Null(d.RescueId));
        Assert.Single(trip.Rescues);
    }

    [Fact]
    public void RecordRescue_TripCompletesWhenTheRescueTruckDeliversTheRest()
    {
        var (trip, drops) = TripWithTwoDropsLeft();
        trip.RecordRescue(Spec(drops.Skip(2)), "office");

        for (var i = 2; i < 4; i++)
        {
            trip.MarkStopArrived(drops[i].Id, arrivedAt: Start.AddHours(15 + i));
            trip.MarkStopDeparted(drops[i].Id, new StopDeparture(LoadStatus.Delivered, DepartedAt: Start.AddHours(15.2 + i)));
        }

        Assert.Equal(TripStatus.Completed, trip.Status);
    }

    [Fact]
    public void RecordRescue_OwnTruck_Throws()
    {
        var (trip, drops) = TripWithTwoDropsLeft();

        Assert.Throws<InvalidOperationException>(() => trip.RecordRescue(Spec(drops.Skip(2), truck: OwnTruck), null));
    }

    [Fact]
    public void RecordRescue_DropAlreadyReached_Throws()
    {
        var (trip, drops) = TripWithTwoDropsLeft();

        var ex = Assert.Throws<InvalidOperationException>(() => trip.RecordRescue(Spec(drops.Skip(1)), null));
        Assert.Contains("Drop 2", ex.Message);
    }

    [Fact]
    public void RecordRescue_DropOnAnotherRescue_Throws()
    {
        var (trip, drops) = TripWithTwoDropsLeft();
        trip.RecordRescue(Spec(drops.Skip(3)), null);

        Assert.Throws<InvalidOperationException>(() => trip.RecordRescue(Spec(drops.Skip(2)), null));
    }

    [Fact]
    public void RecordRescue_DropNotLoadedYet_Throws()
    {
        var loads = Enumerable.Range(1, 2).Select(i => NewLoad($"Drop {i}", i)).ToList();
        var trip = Trip.Create("Two warehouses", OwnTruck, loads);
        trip.Dispatch();
        var firstPickup = trip.Stops.Where(s => s.Type == TripStopType.PickUp).OrderBy(s => s.Order).First();
        trip.MarkStopArrived(firstPickup.Id, arrivedAt: Start.AddHours(5));
        trip.MarkStopDeparted(firstPickup.Id, new StopDeparture(null, DepartedAt: Start.AddHours(6)));
        var secondDrop = trip.Stops.Where(s => s.Type == TripStopType.DropOff).OrderBy(s => s.Order).Last();

        var ex = Assert.Throws<InvalidOperationException>(() => trip.RecordRescue(Spec([secondDrop]), null));
        Assert.Contains("hasn't been loaded", ex.Message);
    }

    [Fact]
    public void RecordRescue_WarehouseStop_Throws()
    {
        var (trip, _) = TripWithTwoDropsLeft();
        var pickup = trip.Stops.First(s => s.Type == TripStopType.PickUp);

        Assert.Throws<InvalidOperationException>(() => trip.RecordRescue(Spec([pickup]), null));
    }

    [Fact]
    public void RecordRescue_BeforeLeavingTheWarehouse_Throws()
    {
        var (trip, drops) = TripWithTwoDropsLeft();

        Assert.Throws<InvalidOperationException>(() => trip.RecordRescue(Spec(drops.Skip(2), at: Start.AddHours(6)), null));
    }

    [Fact]
    public void RecordRescue_DraftTrip_Throws()
    {
        var load = NewLoad("Drop 1", 1);
        var trip = Trip.Create("Draft", OwnTruck, [load]);
        var drop = trip.Stops.Single(s => s.Type == TripStopType.DropOff);

        Assert.Throws<InvalidOperationException>(() => trip.RecordRescue(Spec([drop]), null));
    }

    [Fact]
    public void MarkStopArrived_RescuedDropBeforeTheTransfer_Throws()
    {
        var (trip, drops) = TripWithTwoDropsLeft();
        trip.RecordRescue(Spec(drops.Skip(2)), null);

        Assert.Throws<InvalidOperationException>(() => trip.MarkStopArrived(drops[2].Id, arrivedAt: Start.AddHours(12)));
    }

    [Fact]
    public void RecordGarageTimes_OwnTruckBackBeforeTheRescueFinishes_IsAllowed()
    {
        var (trip, drops) = TripWithTwoDropsLeft();
        trip.RecordRescue(Spec(drops.Skip(2)), null);
        trip.MarkStopArrived(drops[2].Id, arrivedAt: Start.AddHours(16));
        trip.MarkStopDeparted(drops[2].Id, new StopDeparture(LoadStatus.Delivered, DepartedAt: Start.AddHours(16.5)));

        trip.RecordGarageTimes(null, Start.AddHours(14));

        Assert.Equal(Start.AddHours(14), trip.GarageArrivedAt);
    }

    [Fact]
    public void UndoRescue_BeforeAnyRescuedArrival_GivesTheDropsBack()
    {
        var (trip, drops) = TripWithTwoDropsLeft();
        var rescue = trip.RecordRescue(Spec(drops.Skip(2)), null);

        var removed = trip.UndoRescue(rescue.Id);

        Assert.Same(rescue, removed);
        Assert.Empty(trip.Rescues);
        Assert.All(drops, d => Assert.Null(d.RescueId));
    }

    [Fact]
    public void UndoRescue_AfterARescuedArrival_Throws()
    {
        var (trip, drops) = TripWithTwoDropsLeft();
        var rescue = trip.RecordRescue(Spec(drops.Skip(2)), null);
        trip.MarkStopArrived(drops[2].Id, arrivedAt: Start.AddHours(16));

        Assert.Throws<InvalidOperationException>(() => trip.UndoRescue(rescue.Id));
    }

    [Fact]
    public void RecordRescueGarageTimes_AroundTheRescuedDrops_AreKept()
    {
        var (trip, drops) = TripWithTwoDropsLeft();
        var rescue = trip.RecordRescue(Spec(drops.Skip(2)), "office");
        DeliverRescuedDrops(trip, drops);

        trip.RecordRescueGarageTimes(rescue.Id, Start.AddHours(12), Start.AddHours(19.5));

        Assert.Equal(Start.AddHours(12), rescue.GarageDepartedAt);
        Assert.Equal(Start.AddHours(19.5), rescue.GarageArrivedAt);
        Assert.Null(trip.GarageArrivedAt);
    }

    [Fact]
    public void RecordRescueGarageTimes_LeavingAfterTheTransfer_Throws()
    {
        var (trip, drops) = TripWithTwoDropsLeft();
        var rescue = trip.RecordRescue(Spec(drops.Skip(2)), "office");

        Assert.Throws<InvalidOperationException>(() =>
            trip.RecordRescueGarageTimes(rescue.Id, Start.AddHours(14), null));
    }

    [Fact]
    public void RecordRescueGarageTimes_BackBeforeTheLastRescuedDrop_Throws()
    {
        var (trip, drops) = TripWithTwoDropsLeft();
        var rescue = trip.RecordRescue(Spec(drops.Skip(2)), "office");
        DeliverRescuedDrops(trip, drops);

        Assert.Throws<InvalidOperationException>(() =>
            trip.RecordRescueGarageTimes(rescue.Id, null, Start.AddHours(17.5)));
    }

    /// <summary>Drops 3 and 4 reached at 17:00 and 18:00, each left 12 minutes later.</summary>
    private static void DeliverRescuedDrops(Trip trip, IReadOnlyList<TripStop> drops)
    {
        for (var i = 2; i < 4; i++)
        {
            trip.MarkStopArrived(drops[i].Id, arrivedAt: Start.AddHours(15 + i));
            trip.MarkStopDeparted(drops[i].Id, new StopDeparture(LoadStatus.Delivered, DepartedAt: Start.AddHours(15.2 + i)));
        }
    }
}
