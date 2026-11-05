using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>The quotation's conditions around the rate card - see <see cref="RateAgreementTerms"/>.</summary>
public partial class RateAgreement
{
    public FuelAdjustmentMethod FuelMethod { get; private set; }

    /// <summary>Drops the trip rate covers; null means only the first.</summary>
    public int? IncludedDrops { get; private set; }

    /// <summary>Per truck size, what each drop past <see cref="IncludedDrops"/> costs.</summary>
    public virtual List<RateExtraDropCharge> ExtraDropCharges { get; private set; } = [];

    /// <summary>Percentage of the trip rate charged when no drop could be delivered.</summary>
    public decimal? FoulTripPercent { get; private set; }

    /// <summary>Percentage of the trip rate for bringing goods back to the warehouse.</summary>
    public decimal? BackloadPercent { get; private set; }

    public decimal? DemurrageFreeHours { get; private set; }
    public decimal? DemurrageHourlyRate { get; private set; }

    /// <summary>Added for a drop outside the trip's delivery area.</summary>
    public decimal? OtherLocationCharge { get; private set; }

    /// <summary>True when tolls and parking are billed on receipts, false when the rate includes them.</summary>
    public bool? TollsAndParkingAtCost { get; private set; }

    /// <summary>False when extra drops are counted but not charged - see <see cref="RateAgreementTerms"/>.</summary>
    public bool BillExtraDrops { get; private set; } = true;

    /// <summary>True when the card's amounts include VAT, so trips are billed net of it.</summary>
    public bool RatesIncludeVat { get; private set; }

    public RateAgreementTerms Terms => new(
        FuelMethod,
        IncludedDrops,
        ExtraDropCharges.OrderBy(c => c.Tonnage).Select(c => (c.Tonnage, c.Amount)).ToList(),
        FoulTripPercent,
        BackloadPercent,
        DemurrageFreeHours,
        DemurrageHourlyRate,
        OtherLocationCharge,
        TollsAndParkingAtCost,
        BillExtraDrops,
        RatesIncludeVat);

    /// <summary>
    /// Changes how the card is billed without revising it: the rates stay as agreed, only whether
    /// extra drops are charged and whether the amounts include VAT. Allowed on an approved card,
    /// since these are Skybest's billing practice rather than the client's price. Trips already
    /// priced keep their price until they are priced again.
    /// </summary>
    public void SetBillingRules(bool billExtraDrops, bool ratesIncludeVat)
    {
        if (Status is RateAgreementStatus.Withdrawn)
        {
            throw new InvalidOperationException("A withdrawn agreement can't be changed.");
        }

        BillExtraDrops = billExtraDrops;
        RatesIncludeVat = ratesIncludeVat;
    }

    /// <summary>The extra-drop charge for a truck size, falling back to the one for any size.</summary>
    public decimal? ExtraDropChargeFor(TruckTonnageClass? tonnage) =>
        (tonnage is null ? null : ExtraDropCharges.FirstOrDefault(c => c.Tonnage == tonnage)?.Amount)
        ?? ExtraDropCharges.FirstOrDefault(c => c.Tonnage is null)?.Amount;

    private void SetTerms(RateAgreementTerms terms)
    {
        if (terms.IncludedDrops is < 1)
        {
            throw new InvalidOperationException("A trip rate covers at least one drop.");
        }

        RequirePercent(terms.FoulTripPercent, "foul trip");
        RequirePercent(terms.BackloadPercent, "backload");
        RequireNotNegative(terms.DemurrageFreeHours, "demurrage free hours");
        RequireNotNegative(terms.DemurrageHourlyRate, "demurrage rate");
        RequireNotNegative(terms.OtherLocationCharge, "different-location charge");

        var charges = terms.ExtraDropCharges ?? [];
        if (charges.GroupBy(c => c.Tonnage).FirstOrDefault(g => g.Count() > 1) is { } repeated)
        {
            throw new InvalidOperationException(
                $"Two extra-drop charges for {(repeated.Key is null ? "any size" : repeated.Key.ToString())}.");
        }

        foreach (var (_, amount) in charges)
        {
            RequireNotNegative(amount, "extra-drop charge");
        }

        FuelMethod = terms.FuelMethod;
        IncludedDrops = terms.IncludedDrops;
        FoulTripPercent = terms.FoulTripPercent;
        BackloadPercent = terms.BackloadPercent;
        DemurrageFreeHours = terms.DemurrageFreeHours;
        DemurrageHourlyRate = terms.DemurrageHourlyRate;
        OtherLocationCharge = terms.OtherLocationCharge;
        TollsAndParkingAtCost = terms.TollsAndParkingAtCost;
        BillExtraDrops = terms.BillExtraDrops;
        RatesIncludeVat = terms.RatesIncludeVat;

        ExtraDropCharges.Clear();
        foreach (var (tonnage, amount) in charges)
        {
            ExtraDropCharges.Add(new RateExtraDropCharge { RateAgreementId = Id, Tonnage = tonnage, Amount = amount });
        }
    }

    private static void RequirePercent(decimal? value, string term)
    {
        if (value is < 0 or > 100)
        {
            throw new InvalidOperationException($"The {term} charge is a percentage from 0 to 100.");
        }
    }

    private static void RequireNotNegative(decimal? value, string term)
    {
        if (value is < 0)
        {
            throw new InvalidOperationException($"The {term} can't be negative.");
        }
    }
}
