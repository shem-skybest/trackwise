using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

/// <summary>
/// A truck loads everything before it delivers, so a trip's stops list its pickups first and then
/// its drops, each in the order the loads were given.
/// </summary>
public class TripStopOrderTests
{
    private static Load NewLoad(string name) => new()
    {
        Name = name,
        Type = LoadType.RefrigeratedGoods,
        Customer = null!,
        OriginAddress = new Address { StreetAddress = "JM WHSE", CityOrMunicipality = "Makati", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        OriginLocation = new GeoPoint(121.02, 14.55),
        DestinationAddress = new Address { StreetAddress = name, CityOrMunicipality = "Pasay", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        DestinationLocation = new GeoPoint(121.0, 14.52),
        DeliveryCost = Money.Zero("PHP")
    };

    private static List<(TripStopType Type, string Load)> StopsOf(Trip trip) =>
        trip.Stops.OrderBy(s => s.Order).Select(s => (s.Type, s.Load.Name)).ToList();

    [Fact]
    public void Create_ListsEveryPickupBeforeTheDrops()
    {
        var trip = Trip.Create("ABD6775 - ALDER", null, [NewLoad("GRAND BAY RESORT"), NewLoad("HARBOR HOTEL"), NewLoad("PRIME RESTO")]);

        Assert.Equal(
            [
                (TripStopType.PickUp, "GRAND BAY RESORT"), (TripStopType.PickUp, "HARBOR HOTEL"), (TripStopType.PickUp, "PRIME RESTO"),
                (TripStopType.DropOff, "GRAND BAY RESORT"), (TripStopType.DropOff, "HARBOR HOTEL"), (TripStopType.DropOff, "PRIME RESTO")
            ],
            StopsOf(trip));
        Assert.Equal([1, 2, 3, 4, 5, 6], trip.Stops.OrderBy(s => s.Order).Select(s => s.Order));
    }

    [Fact]
    public void AddLoads_AppendsAfterTheExistingStops()
    {
        var trip = Trip.Create("ABD6775 - ALDER", null, [NewLoad("GRAND BAY RESORT")]);

        trip.AddLoads([NewLoad("PLAZA NORTH")]);

        Assert.Equal(
            [
                (TripStopType.PickUp, "GRAND BAY RESORT"), (TripStopType.DropOff, "GRAND BAY RESORT"),
                (TripStopType.PickUp, "PLAZA NORTH"), (TripStopType.DropOff, "PLAZA NORTH")
            ],
            StopsOf(trip));
    }
}
