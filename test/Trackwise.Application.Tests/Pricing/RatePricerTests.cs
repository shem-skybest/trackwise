using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.Pricing;

/// <summary>
/// The truck-trip pricing rules, on rate cards shaped like the real Monday boards: HFI's (town and
/// province rows per truck size, diesel tiers), PFI's (distance bands) and Bayview's (a fuel %).
/// </summary>
public class RatePricerTests
{
    private static readonly Guid DropA = Guid.NewGuid();
    private static readonly Guid DropB = Guid.NewGuid();
    private static readonly Guid DropC = Guid.NewGuid();

    private static RateLineSpec Place(string label, TruckTonnageClass? size, decimal amount, string? city, string? province,
        decimal? fuelPercent = null, params (decimal, decimal)[] tiers) =>
        new(label, size, amount, null, null, city, province, null, fuelPercent, tiers);

    private static RateLineSpec Band(string label, TruckTonnageClass size, decimal amount, int min, int max,
        params (decimal, decimal)[] tiers) =>
        new(label, size, amount, min, max, null, null, null, null, tiers);

    private static RateAgreement Agreement(FuelAdjustmentMethod fuel, decimal? extraDrop, params RateLineSpec[] lines) =>
        Agreement(new RateAgreementTerms(fuel, ExtraDropCharges: extraDrop is { } charge ? [(null, charge)] : null), lines);

    private static RateAgreement Agreement(RateAgreementTerms terms, params RateLineSpec[] lines)
    {
        var agreement = RateAgreement.Create(Guid.NewGuid(), "Test rates", new DateOnly(2026, 3, 13), null, terms, null);
        agreement.ReplaceLines(lines);
        agreement.Approve("tester", DateTime.UtcNow);
        return agreement;
    }

    private static RateAgreement HFI() => Agreement(
        FuelAdjustmentMethod.DieselTierSurcharge, 300,
        Place("QUEZON CITY", TruckTonnageClass.TwoTonner, 11200, "Quezon City", "Metro Manila", null, (85, 1250), (100, 2250), (120, 3400)),
        Place("QUEZON CITY", TruckTonnageClass.FourTonner, 14000, "Quezon City", "Metro Manila", null, (85, 1250), (100, 2900), (120, 4400)),
        Place("MUNTINLUPA", TruckTonnageClass.TwoTonner, 9300, "Muntinlupa", "Metro Manila", null, (85, 930), (100, 1700), (120, 2600)),
        Place("STA. ROSA LAGUNA", TruckTonnageClass.TwoTonner, 8500, "Sta. Rosa", "Laguna", null, (85, 770)),
        Place("RIZAL", TruckTonnageClass.TwoTonner, 10500, null, "Rizal", null, (85, 1150)));

    private static RatedDrop Drop(Guid id, string key, string city, string province, double? km = null) =>
        new(id, key, city, province, km);

    [Fact]
    public void ATownRow_PricesTheDrop_ForTheTrucksSize()
    {
        var quote = RatePricer.Price(HFI(), TruckTonnageClass.FourTonner, 92m, [Drop(DropA, "a", "Quezon City", "Metro Manila")]);

        Assert.Equal(14000, quote.BaseAmount);
        Assert.Equal(1250, quote.FuelAmount);
        Assert.Equal(15250, quote.Total);
    }

    [Theory]
    [InlineData("Santa Rosa", "Laguna")]
    [InlineData("City of Santa Rosa", "Laguna")]
    [InlineData("STA. ROSA", "LAGUNA")]
    public void TownNames_MatchAcrossSpellings(string city, string province)
    {
        var quote = RatePricer.Price(HFI(), TruckTonnageClass.TwoTonner, 90m, [Drop(DropA, "a", city, province)]);

        Assert.Equal("STA. ROSA LAGUNA", quote.LineLabel);
    }

    [Fact]
    public void AProvinceRow_CoversATownTheCardDoesNotList()
    {
        var quote = RatePricer.Price(HFI(), TruckTonnageClass.TwoTonner, 90m, [Drop(DropA, "a", "Antipolo", "Rizal")]);

        Assert.Equal("RIZAL", quote.LineLabel);
    }

    [Theory]
    [InlineData(84.99, 0)]
    [InlineData(85, 1250)]
    [InlineData(99.99, 1250)]
    [InlineData(100, 2250)]
    [InlineData(150, 3400)]
    public void TheDieselTier_IsTheHighestOneAtOrBelowThePrice(decimal diesel, decimal expectedFuel)
    {
        var quote = RatePricer.Price(HFI(), TruckTonnageClass.TwoTonner, diesel, [Drop(DropA, "a", "Quezon City", "Metro Manila")]);

        Assert.Equal(expectedFuel, quote.FuelAmount);
    }

    [Fact]
    public void WithoutADieselPrice_NoFuelIsAdded_AndItSaysSo()
    {
        var quote = RatePricer.Price(HFI(), TruckTonnageClass.TwoTonner, null, [Drop(DropA, "a", "Quezon City", "Metro Manila")]);

        Assert.Equal(0, quote.FuelAmount);
        Assert.Contains(quote.Warnings, w => w.Contains("diesel price"));
    }

    [Fact]
    public void AMultiDropTrip_IsTheCostliestDrop_PlusExtraDrops()
    {
        var quote = RatePricer.Price(HFI(), TruckTonnageClass.TwoTonner, 90m,
        [
            Drop(DropA, "muntinlupa-school", "Muntinlupa", "Metro Manila"),
            Drop(DropB, "qc-school", "Quezon City", "Metro Manila"),
            Drop(DropC, "muntinlupa-school", "Muntinlupa", "Metro Manila")
        ]);

        Assert.Equal("QUEZON CITY", quote.LineLabel);
        Assert.Equal(1, quote.ExtraDrops);
        Assert.Equal(11200 + 1250 + 300, quote.Total);

        // The loads carry the price between them, so the trip's revenue is the truck-trip price.
        Assert.Equal(12450, quote.ChargePerLoad[DropB]);
        Assert.Equal(300, quote.ChargePerLoad[DropA]);
        Assert.Equal(0, quote.ChargePerLoad[DropC]);
        Assert.Equal(quote.Total, quote.ChargePerLoad.Values.Sum());
    }

    [Fact]
    public void AnUnlistedPlace_IsRefused_NamingIt()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            RatePricer.Price(HFI(), TruckTonnageClass.TwoTonner, 90m, [Drop(DropA, "a", "Lipa", "Batangas")]));

        Assert.Contains("Lipa", ex.Message);
    }

    [Fact]
    public void ATruckWithNoSize_OnlyMatchesAnySizeRows()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RatePricer.Price(HFI(), null, 90m, [Drop(DropA, "a", "Quezon City", "Metro Manila")]));
    }

    [Theory]
    [InlineData(5, "10 - 40 KM")]
    [InlineData(40, "10 - 40 KM")]
    [InlineData(40.2, "41 - 60 KM")]
    [InlineData(60, "41 - 60 KM")]
    public void ADistanceCard_PricesByTheLoadsRouteKm(double km, string expectedBand)
    {
        var pfi = Agreement(FuelAdjustmentMethod.DieselTierSurcharge, null,
            Band("10 - 40 KM", TruckTonnageClass.TwoTonner, 6400, 10, 40, (85, 220)),
            Band("41 - 60 KM", TruckTonnageClass.TwoTonner, 7200, 41, 60, (85, 300)));

        var quote = RatePricer.Price(pfi, TruckTonnageClass.TwoTonner, 90m, [Drop(DropA, "a", "Pasig", "Metro Manila", km)]);

        Assert.Equal(expectedBand, quote.LineLabel);
    }

    [Fact]
    public void BeyondTheLastBand_IsRefused()
    {
        var pfi = Agreement(FuelAdjustmentMethod.None, null, Band("10 - 40 KM", TruckTonnageClass.TwoTonner, 6400, 10, 40));

        Assert.Throws<InvalidOperationException>(() =>
            RatePricer.Price(pfi, TruckTonnageClass.TwoTonner, null, [Drop(DropA, "a", "Baguio", "Benguet", 250)]));
    }

    [Fact]
    public void APercentageCard_AddsItsFuelPercentOfTheBase()
    {
        var bayview = Agreement(FuelAdjustmentMethod.Percentage, null,
            Place("HAGONOY, BULACAN", TruckTonnageClass.TwoTonner, 4750, "Hagonoy", "Bulacan", 5));

        var quote = RatePricer.Price(bayview, TruckTonnageClass.TwoTonner, null, [Drop(DropA, "a", "Hagonoy", "Bulacan")]);

        Assert.Equal(237.5m, quote.FuelAmount);
        Assert.Equal(4987.5m, quote.Total);
    }

    private static RateAgreement MetroManilaCard(RateAgreementTerms terms) => Agreement(terms,
        Place("METRO MANILA", TruckTonnageClass.TwoTonner, 6700, null, "Metro Manila"),
        Place("METRO MANILA", TruckTonnageClass.FourTonner, 8400, null, "Metro Manila"),
        Place("METRO MANILA", null, 7000, null, "Metro Manila"));

    private static readonly RateAgreementTerms AlderTerms = new(
        FuelAdjustmentMethod.None,
        IncludedDrops: 2,
        ExtraDropCharges: [(TruckTonnageClass.TwoTonner, 750m), (TruckTonnageClass.FourTonner, 1500m)],
        FoulTripPercent: 50);

    private static RatedDrop[] ThreeOutlets(bool refused = false) =>
    [
        new(DropA, "summit hotel", "Mandaluyong", "Metro Manila", null, refused),
        new(DropB, "crowne-plaza", "Pasig", "Metro Manila", null, refused),
        new(DropC, "harbor hotel", "Pasay", "Metro Manila", null, refused)
    ];

    [Theory]
    [InlineData(TruckTonnageClass.TwoTonner, 6700, 750)]
    [InlineData(TruckTonnageClass.FourTonner, 8400, 1500)]
    public void DropsPastTheIncludedOnes_AreChargedAtTheTrucksSize(TruckTonnageClass size, decimal rate, decimal extra)
    {
        var quote = RatePricer.Price(MetroManilaCard(AlderTerms), size, null, ThreeOutlets());

        // Two drops are in the rate; the third outlet in stop order is the extra one.
        Assert.Equal(1, quote.ExtraDrops);
        Assert.Equal(rate + extra, quote.Total);
        Assert.Equal(0, quote.ChargePerLoad[DropB]);
        Assert.Equal(extra, quote.ChargePerLoad[DropC]);
        Assert.Equal(quote.Total, quote.ChargePerLoad.Values.Sum());
    }

    [Fact]
    public void WhenTheAgreementDoesNotBillExtraDrops_TheyAreCountedButFree_AndItSaysSo()
    {
        var quote = RatePricer.Price(
            MetroManilaCard(AlderTerms with { BillExtraDrops = false }), TruckTonnageClass.TwoTonner, null, ThreeOutlets());

        Assert.Equal(1, quote.ExtraDrops);
        Assert.Equal(0, quote.ExtraDropAmount);
        Assert.Equal(6700, quote.Total);
        Assert.Equal(quote.Total, quote.ChargePerLoad.Values.Sum());
        Assert.Contains(quote.Warnings, w => w.Contains("doesn't bill extra drops"));
    }

    [Fact]
    public void AVatInclusiveCard_IsBilledNetOfVat_RateFuelAndExtraDrops()
    {
        var global = Agreement(
            new RateAgreementTerms(
                FuelAdjustmentMethod.DieselTierSurcharge,
                IncludedDrops: 1,
                ExtraDropCharges: [(null, 700m)],
                RatesIncludeVat: true),
            Place("TAYTAY", TruckTonnageClass.TwoTonner, 11500, "Taytay", "Rizal", null, (85, 1120)),
            Place("ANTIPOLO", TruckTonnageClass.TwoTonner, 11000, "Antipolo", "Rizal", null, (85, 1120)));

        var quote = RatePricer.Price(global, TruckTonnageClass.TwoTonner, 90m,
            [Drop(DropA, "a", "Taytay", "Rizal"), Drop(DropB, "b", "Antipolo", "Rizal")]);

        Assert.Equal(10267.86m, quote.BaseAmount);
        Assert.Equal(1000m, quote.FuelAmount);
        Assert.Equal(625m, quote.ExtraDropAmount);
        Assert.Contains("billed net", quote.FuelBasis);
    }

    [Fact]
    public void ASizeWithNoExtraDropCharge_FallsBackToTheAnySizeOne_OrSaysSo()
    {
        var withAnySize = AlderTerms with
        {
            ExtraDropCharges = [(TruckTonnageClass.TwoTonner, 750m), (null, 1000m)]
        };

        var fallback = RatePricer.Price(MetroManilaCard(withAnySize), TruckTonnageClass.ThreeTonner, null, ThreeOutlets());
        var missing = RatePricer.Price(MetroManilaCard(AlderTerms), TruckTonnageClass.ThreeTonner, null, ThreeOutlets());

        Assert.Equal(7000 + 1000, fallback.Total);
        Assert.Equal(7000, missing.Total);
        Assert.Contains(missing.Warnings, w => w.Contains("extra-drop charge"));
    }

    [Fact]
    public void WhenNothingWasDelivered_TheFoulTripShareOfTheRateIsCharged_WithNoExtraDrops()
    {
        var quote = RatePricer.Price(MetroManilaCard(AlderTerms), TruckTonnageClass.TwoTonner, null, ThreeOutlets(refused: true));

        Assert.Equal(50, quote.FoulTripPercent);
        Assert.Equal(3350, quote.BaseAmount);
        Assert.Equal(0, quote.ExtraDrops);
        Assert.Equal(3350, quote.Total);
        Assert.Equal(quote.Total, quote.ChargePerLoad.Values.Sum());
    }

    [Fact]
    public void TheFoulTripShare_AppliesToTheFuelToo()
    {
        var hfi = Agreement(
            new RateAgreementTerms(FuelAdjustmentMethod.DieselTierSurcharge, FoulTripPercent: 50),
            Place("QUEZON CITY", TruckTonnageClass.TwoTonner, 11200, "Quezon City", "Metro Manila", null, (85, 1250)));

        var quote = RatePricer.Price(hfi, TruckTonnageClass.TwoTonner, 90m,
            [new RatedDrop(DropA, "qc", "Quezon City", "Metro Manila", null, Refused: true)]);

        Assert.Equal(5600, quote.BaseAmount);
        Assert.Equal(625, quote.FuelAmount);
    }

    [Fact]
    public void OneRefusedDrop_OnATripThatDeliveredElsewhere_IsPricedNormally()
    {
        var drops = ThreeOutlets();
        drops[0] = drops[0] with { Refused = true };

        var quote = RatePricer.Price(MetroManilaCard(AlderTerms), TruckTonnageClass.TwoTonner, null, drops);

        Assert.Null(quote.FoulTripPercent);
        Assert.Equal(6700 + 750, quote.Total);
    }

    [Fact]
    public void WhenNothingWasDelivered_ButNoFoulTripTermIsRecorded_TheFullRateAppliesAndItSaysSo()
    {
        var quote = RatePricer.Price(MetroManilaCard(AlderTerms with { FoulTripPercent = null }),
            TruckTonnageClass.TwoTonner, null, ThreeOutlets(refused: true));

        Assert.Null(quote.FoulTripPercent);
        Assert.Equal(6700, quote.Total);
        Assert.Contains(quote.Warnings, w => w.Contains("foul-trip"));
    }
}
