using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

/// <summary>The terms and rate card a draft agreement is created or edited with.</summary>
public abstract class RateAgreementTermsCommand
{
    public string Name { get; set; } = default!;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public FuelAdjustmentMethod FuelMethod { get; set; }

    /// <summary>Drops the trip rate covers; null means one.</summary>
    public int? IncludedDrops { get; set; }

    /// <summary>What each further drop costs, per truck size (a null size covers any).</summary>
    public List<RateExtraDropChargeDto> ExtraDropCharges { get; set; } = [];

    /// <summary>Percentage of the trip rate charged when nothing could be delivered.</summary>
    public decimal? FoulTripPercent { get; set; }

    /// <summary>Percentage of the trip rate for bringing goods back to the warehouse.</summary>
    public decimal? BackloadPercent { get; set; }

    public decimal? DemurrageFreeHours { get; set; }
    public decimal? DemurrageHourlyRate { get; set; }

    /// <summary>Added for a drop outside the trip's delivery area.</summary>
    public decimal? OtherLocationCharge { get; set; }

    public bool? TollsAndParkingAtCost { get; set; }

    /// <summary>False when extra drops are counted but not charged, as SAS bills today.</summary>
    public bool BillExtraDrops { get; set; }

    /// <summary>True when the card's amounts include VAT; trips are then billed net.</summary>
    public bool RatesIncludeVat { get; set; }

    public string? Notes { get; set; }
    public List<RateAgreementLineDto> Lines { get; set; } = [];

    internal RateAgreementTerms ToTerms() => new(
        FuelMethod,
        IncludedDrops,
        ExtraDropCharges.Select(c => (c.Tonnage, c.Amount)).ToList(),
        FoulTripPercent,
        BackloadPercent,
        DemurrageFreeHours,
        DemurrageHourlyRate,
        OtherLocationCharge,
        TollsAndParkingAtCost,
        BillExtraDrops,
        RatesIncludeVat);
}
