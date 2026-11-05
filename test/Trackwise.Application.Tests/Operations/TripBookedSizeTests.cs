using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

/// <summary>
/// A Cold Chain trip bills the truck size the client booked, not the truck that ran it: clients
/// load a 4-tonner sent on a 2-tonner booking past the 2 tons they paid for.
/// </summary>
public class TripBookedSizeTests
{
    private static Truck NewTruck(string plate, TruckTonnageClass tonnage) =>
        new() { Number = plate, Name = plate, Type = TruckType.Reefer, TonnageClass = tonnage };

    private static Booking BookedAs(TruckTonnageClass size)
    {
        var agreement = RateAgreement.Create(
            Guid.NewGuid(), "CLEARWATER 2026", new DateOnly(2026, 1, 1), null,
            new RateAgreementTerms(FuelAdjustmentMethod.DieselTierSurcharge), null);
        agreement.ReplaceLines([
            new RateLineSpec("QUEZON CITY", size, 9000, null, null, "Quezon City", "Metro Manila", null, null, [])
        ]);
        agreement.Approve("manager", DateTime.UtcNow);

        return Booking.CreateFromRateAgreement(agreement, "BKN-RFV1331-26-M", new ColdChainBookingPlan(
            new DateOnly(2026, 10, 7), null, size, Guid.NewGuid(), null, null, null, null,
            [new BookingDropSpec(Guid.NewGuid(), 60, null, null)]));
    }

    private static Load NewLoad(Booking? booking) => new()
    {
        Name = "Drop",
        Type = LoadType.RefrigeratedGoods,
        Customer = null!,
        Booking = booking,
        BookingId = booking?.Id,
        OriginAddress = new Address { StreetAddress = "1 A", CityOrMunicipality = "Makati", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        OriginLocation = new GeoPoint(121.02, 14.55),
        DestinationAddress = new Address { StreetAddress = "2 B", CityOrMunicipality = "Quezon City", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        DestinationLocation = new GeoPoint(121.04, 14.65),
        DeliveryCost = Money.Zero("PHP")
    };

    private static void Price(Trip trip)
    {
        var charges = trip.GetLoads().ToDictionary(l => l.Id, _ => 9000m);
        var quote = new TripRateQuote(
            Guid.NewGuid(), "CLEARWATER 2026", charges.Keys.First(), "QUEZON CITY", 9000m, 0m, "", 0, 0m, charges, []);
        trip.ApplyRate(quote, new DateOnly(2026, 10, 7), DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void PricingSize_TwoTonnerBookingOnAFourTonner_IsTheBookedSize()
    {
        var booking = BookedAs(TruckTonnageClass.TwoTonner);
        var trip = Trip.Create("ABF3726 - CLEARWATER", NewTruck("ABF3726", TruckTonnageClass.FourTonner), [NewLoad(booking)]);

        Assert.Equal(TruckTonnageClass.TwoTonner, trip.BookedTruckSize);
        Assert.Equal(TruckTonnageClass.TwoTonner, trip.PricingSize);
    }

    [Fact]
    public void PricingSize_DropsWithoutABooking_IsTheTrucksSize()
    {
        var trip = Trip.Create("ABF3726", NewTruck("ABF3726", TruckTonnageClass.FourTonner), [NewLoad(null)]);

        Assert.Null(trip.BookedTruckSize);
        Assert.Equal(TruckTonnageClass.FourTonner, trip.PricingSize);
    }

    [Fact]
    public void PricingSize_DropsBookedAtDifferentSizes_IsTheTrucksSize()
    {
        var trip = Trip.Create(
            "ABF3726",
            NewTruck("ABF3726", TruckTonnageClass.FourTonner),
            [NewLoad(BookedAs(TruckTonnageClass.TwoTonner)), NewLoad(BookedAs(TruckTonnageClass.FourTonner))]);

        Assert.Null(trip.BookedTruckSize);
        Assert.Equal(TruckTonnageClass.FourTonner, trip.PricingSize);
    }

    [Fact]
    public void AssignToTruck_BookedTripOnABiggerTruck_KeepsThePrice()
    {
        var trip = Trip.Create(
            "ABD6777 - CLEARWATER", NewTruck("ABD6777", TruckTonnageClass.TwoTonner), [NewLoad(BookedAs(TruckTonnageClass.TwoTonner))]);
        Price(trip);

        trip.AssignToTruck(NewTruck("ABF3726", TruckTonnageClass.FourTonner));

        Assert.False(trip.IsPriceOutdated);
    }
}
