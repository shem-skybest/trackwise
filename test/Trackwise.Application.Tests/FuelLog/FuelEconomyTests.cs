using Trackwise.Application.Modules.Financial.FuelLog;
using Xunit;

namespace Trackwise.Application.Tests.FuelLog;

/// <summary>
/// km per liter is worked out from odometer readings, full tank to full tank, instead of Monday's
/// typed daily mileage.
/// </summary>
public class FuelEconomyTests
{
    private static readonly Guid Truck = Guid.NewGuid();
    private static readonly DateTime Sept1 = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    private static long number;

    private static FuelReading Fill(int day, decimal liters, int? odometer, Guid? truck = null) =>
        new(Guid.NewGuid(), truck ?? Truck, Sept1.AddDays(day), ++number, liters, odometer);

    [Fact]
    public void Legs_FirstReading_ClosesNoLeg()
    {
        var first = Fill(0, 40, 10_000);

        Assert.Empty(FuelEconomy.Legs([first]));
    }

    [Fact]
    public void Legs_NextReading_KmOverItsLiters()
    {
        var second = Fill(1, 50, 10_400);

        var legs = FuelEconomy.Legs([Fill(0, 40, 10_000), second]);

        Assert.Equal(400, legs[second.Id].Km);
        Assert.Equal(50, legs[second.Id].Liters);
        Assert.Equal(8, legs[second.Id].KmPerLiter);
    }

    [Fact]
    public void Legs_FillUpWithoutReading_AddsItsLitersToTheNextLeg()
    {
        var unread = Fill(1, 30, null);
        var closing = Fill(2, 20, 10_400);

        var legs = FuelEconomy.Legs([Fill(0, 40, 10_000), unread, closing]);

        Assert.False(legs.ContainsKey(unread.Id));
        Assert.Equal(50, legs[closing.Id].Liters);
        Assert.Equal(8, legs[closing.Id].KmPerLiter);
    }

    [Fact]
    public void Legs_SameReadingTwice_TopUpCarriesIntoTheNextLeg()
    {
        var topUp = Fill(1, 10, 10_000);
        var closing = Fill(2, 40, 10_400);

        var legs = FuelEconomy.Legs([Fill(0, 40, 10_000), topUp, closing]);

        Assert.False(legs.ContainsKey(topUp.Id));
        Assert.Equal(50, legs[closing.Id].Liters);
    }

    [Fact]
    public void Legs_ReadingLowerThanTheLast_ClosesNoLeg()
    {
        var typo = Fill(1, 40, 9_000);

        Assert.False(FuelEconomy.Legs([Fill(0, 40, 10_000), typo]).ContainsKey(typo.Id));
    }

    [Fact]
    public void Legs_OtherTruck_KeepsItsOwnReadings()
    {
        var other = Guid.NewGuid();
        var otherFirst = Fill(1, 40, 55_000, other);

        var legs = FuelEconomy.Legs([Fill(0, 40, 10_000), otherFirst, Fill(2, 40, 10_300)]);

        Assert.False(legs.ContainsKey(otherFirst.Id));
        Assert.Single(legs);
    }

    [Fact]
    public void Legs_OutOfOrderInput_SortedByWhenFilled()
    {
        var later = Fill(5, 25, 10_200);
        var earlier = Fill(0, 40, 10_000);

        var legs = FuelEconomy.Legs([later, earlier]);

        Assert.Equal(200, legs[later.Id].Km);
    }
}
