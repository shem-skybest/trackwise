using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

/// <summary>
/// The crew report edits the trip's truck, crew and drops in place, but only while they are still
/// the plan: once a stop's arrival is recorded the trip is the record of who ran it, and a drop
/// that was reached can't be deleted. Truck size and drops feed the price, so changing them after
/// pricing flags it out of date.
/// </summary>
public class TripHeaderLockTests
{
    private readonly Employee driver = new() { Code = "", Email = "fely@crew.test", FirstName = "Fely", LastName = "Moreno" };

    private static Truck NewTruck(string plate, TruckTonnageClass tonnage) =>
        new() { Number = plate, Name = plate, Type = TruckType.Reefer, TonnageClass = tonnage };

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

    private (Trip trip, Load first, Load second) DispatchedTrip()
    {
        var first = NewLoad("Vista Prime");
        var second = NewLoad("Corner Grocer");
        var trip = Trip.Create("ABE4715 - ALDER", NewTruck("ABE4715", TruckTonnageClass.TwoTonner), [first, second], driver: driver);
        trip.Dispatch();
        return (trip, first, second);
    }

    private static void Price(Trip trip)
    {
        var charges = trip.GetLoads().ToDictionary(l => l.Id, _ => 4000m);
        var quote = new TripRateQuote(
            Guid.NewGuid(), "ALDER 2026", charges.Keys.First(), "MAKATI", 8000m, 0m, "", 0, 0m, charges, []);
        trip.ApplyRate(quote, DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow.AddMinutes(-1));
    }

    private static TripStop DropOf(Trip trip, Load load) =>
        trip.Stops.Single(s => s.LoadId == load.Id && s.Type == TripStopType.DropOff);

    [Fact]
    public void BeforeAnythingIsRecorded_TruckCrewAndDropsCanChange()
    {
        var (trip, _, second) = DispatchedTrip();

        trip.AssignToTruck(NewTruck("ABC1267", TruckTonnageClass.TwoTonner));
        trip.AssignCrew(null, null, null);
        trip.RemoveLoad(second.Id);

        Assert.Equal("ABC1267", trip.Truck!.Number);
        Assert.Null(trip.DriverId);
        Assert.DoesNotContain(trip.Stops, s => s.LoadId == second.Id);
    }

    [Fact]
    public void ADropArrivalWithoutTheWarehouse_StillLocksTruckAndCrew()
    {
        var (trip, first, _) = DispatchedTrip();
        trip.MarkStopArrived(DropOf(trip, first).Id, arrivedAt: DateTime.UtcNow.AddHours(-1));

        Assert.Equal(TripStatus.Dispatched, trip.Status);
        Assert.Throws<InvalidOperationException>(() => trip.AssignToTruck(NewTruck("ABC1267", TruckTonnageClass.TwoTonner)));
        Assert.Throws<InvalidOperationException>(() => trip.AssignCrew(null, null, null));
    }

    [Fact]
    public void RemoveLoad_RefusesAReachedDrop_ButNotAnUnvisitedOne()
    {
        var (trip, first, second) = DispatchedTrip();
        trip.MarkStopArrived(DropOf(trip, first).Id, arrivedAt: DateTime.UtcNow.AddHours(-1));

        Assert.Throws<InvalidOperationException>(() => trip.RemoveLoad(first.Id));
        trip.RemoveLoad(second.Id);

        Assert.DoesNotContain(trip.Stops, s => s.LoadId == second.Id);
    }

    [Fact]
    public void RemovingADropAfterPricing_FlagsThePrice()
    {
        var (trip, _, second) = DispatchedTrip();
        Price(trip);

        trip.RemoveLoad(second.Id);

        Assert.True(trip.IsPriceOutdated);
    }

    [Theory]
    [InlineData(TruckTonnageClass.TwoTonner, false)]
    [InlineData(TruckTonnageClass.FourTonner, true)]
    public void ChangingTheTruckAfterPricing_FlagsThePriceOnlyWhenTheSizeChanges(TruckTonnageClass size, bool outdated)
    {
        var (trip, _, _) = DispatchedTrip();
        Price(trip);

        trip.AssignToTruck(NewTruck("ABF3726", size));

        Assert.Equal(outdated, trip.IsPriceOutdated);
    }

    [Fact]
    public void ChangesBeforePricing_DontFlagAnything()
    {
        var (trip, _, second) = DispatchedTrip();

        trip.RemoveLoad(second.Id);
        trip.AssignToTruck(NewTruck("ABF3726", TruckTonnageClass.FourTonner));

        Assert.Null(trip.PriceOutdatedSince);
    }
}
