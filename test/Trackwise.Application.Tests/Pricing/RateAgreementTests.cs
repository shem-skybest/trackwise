using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.Pricing;

/// <summary>
/// A price change is a new revision, never an edit, so trips keep the rates they were priced on.
/// </summary>
public class RateAgreementTests
{
    private static readonly RateLineSpec QuezonCity =
        new("QUEZON CITY", TruckTonnageClass.TwoTonner, 11200, null, null, "Quezon City", "Metro Manila", null, null, [(85m, 1250m)]);

    private static RateAgreement Draft(DateOnly from)
    {
        var agreement = RateAgreement.Create(Guid.NewGuid(), "HFI rates", from, null, new RateAgreementTerms(FuelAdjustmentMethod.DieselTierSurcharge), null);
        agreement.ReplaceLines([QuezonCity]);
        return agreement;
    }

    [Fact]
    public void ADraftWithNoRates_CannotBeApproved()
    {
        var agreement = RateAgreement.Create(Guid.NewGuid(), "Empty", new DateOnly(2026, 3, 13), null, new RateAgreementTerms(FuelAdjustmentMethod.None), null);

        Assert.Throws<InvalidOperationException>(() => agreement.Approve("manager", DateTime.UtcNow));
    }

    [Fact]
    public void AnApprovedAgreement_CannotBeEdited()
    {
        var agreement = Draft(new DateOnly(2026, 3, 13));
        agreement.Approve("manager", DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => agreement.ReplaceLines([QuezonCity]));
        Assert.Throws<InvalidOperationException>(() => agreement.UpdateTerms(
            "Renamed", new DateOnly(2026, 3, 13), null, new RateAgreementTerms(FuelAdjustmentMethod.None), null));
    }

    [Fact]
    public void ARevision_CopiesTheRates_IntoANewDraft()
    {
        var march = Draft(new DateOnly(2026, 3, 13));
        march.Approve("manager", DateTime.UtcNow);

        var september = march.CreateRevision("HFI rates September 21, 2026", new DateOnly(2026, 9, 21));

        Assert.Equal(RateAgreementStatus.Draft, september.Status);
        Assert.Equal(march.Id, september.SupersedesId);
        var line = Assert.Single(september.Lines);
        Assert.Equal(11200, line.Amount);
        Assert.Equal(1250, Assert.Single(line.FuelSurcharges).Amount);
        Assert.NotEqual(march.Lines[0].Id, line.Id);
    }

    [Fact]
    public void Superseding_ClosesTheDayBeforeTheReplacementStarts()
    {
        var march = Draft(new DateOnly(2026, 3, 13));
        march.Approve("manager", DateTime.UtcNow);

        march.SupersedeFrom(new DateOnly(2026, 9, 21));

        Assert.Equal(RateAgreementStatus.Superseded, march.Status);
        Assert.Equal(new DateOnly(2026, 9, 20), march.EffectiveTo);
        Assert.True(march.IsInForceOn(new DateOnly(2026, 9, 20)));
        Assert.False(march.IsInForceOn(new DateOnly(2026, 9, 21)));
    }

    [Theory]
    [InlineData(10, 40, null, null)]
    [InlineData(null, null, null, null)]
    public void ALine_IsEitherABandOrAPlace(int? min, int? max, string? city, string? province)
    {
        var both = new RateLineSpec("BAD", null, 5000, min, max, city ?? (min is null ? null : "Pasig"), province, null, null, null);
        var agreement = RateAgreement.Create(Guid.NewGuid(), "x", new DateOnly(2026, 1, 1), null, new RateAgreementTerms(FuelAdjustmentMethod.None), null);

        Assert.Throws<InvalidOperationException>(() => agreement.ReplaceLines([both]));
    }

    [Fact]
    public void ARevision_CarriesTheQuotationTerms()
    {
        var terms = new RateAgreementTerms(
            FuelAdjustmentMethod.None,
            IncludedDrops: 5,
            ExtraDropCharges: [(TruckTonnageClass.TwoTonner, 750m), (TruckTonnageClass.FourTonner, 1500m)],
            FoulTripPercent: 50,
            BackloadPercent: 50,
            DemurrageFreeHours: 2,
            DemurrageHourlyRate: 250,
            OtherLocationCharge: 2000,
            TollsAndParkingAtCost: false);
        var agreement = RateAgreement.Create(Guid.NewGuid(), "HFI rates", new DateOnly(2026, 9, 3), null, terms, null);
        agreement.ReplaceLines([new RateLineSpec("QUEZON CITY", TruckTonnageClass.TwoTonner, 14300, null, null, "Quezon City", "Metro Manila", null, null, null)]);
        agreement.Approve("manager", DateTime.UtcNow);

        var revision = agreement.CreateRevision("HFI rates 2027", new DateOnly(2027, 1, 1));

        Assert.Equal(5, revision.IncludedDrops);
        Assert.Equal(1500m, revision.ExtraDropChargeFor(TruckTonnageClass.FourTonner));
        Assert.Equal(50, revision.FoulTripPercent);
        Assert.Equal(2, revision.DemurrageFreeHours);
        Assert.Equal(2000, revision.OtherLocationCharge);
        Assert.False(revision.TollsAndParkingAtCost);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(null, 120)]
    public void ImpossibleTerms_AreRefused(int? includedDrops, int? foulTripPercent)
    {
        Assert.Throws<InvalidOperationException>(() => RateAgreement.Create(
            Guid.NewGuid(), "x", new DateOnly(2026, 1, 1), null,
            new RateAgreementTerms(FuelAdjustmentMethod.None, includedDrops, FoulTripPercent: foulTripPercent), null));
    }

    [Fact]
    public void TwoExtraDropChargesForOneSize_AreRefused()
    {
        Assert.Throws<InvalidOperationException>(() => RateAgreement.Create(
            Guid.NewGuid(), "x", new DateOnly(2026, 1, 1), null,
            new RateAgreementTerms(FuelAdjustmentMethod.None,
                ExtraDropCharges: [(TruckTonnageClass.TwoTonner, 750m), (TruckTonnageClass.TwoTonner, 700m)]),
            null));
    }
}
