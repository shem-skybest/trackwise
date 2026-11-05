namespace Trackwise.DbMigrator.Models;

/// <summary>
/// One dated revision of a client's rate card, read from a Monday quotation board group (e.g.
/// "HFI NEW RATES MARCH 13, 2026"), seeded by <c>RateAgreementSeeder</c>.
/// </summary>
public record RateAgreementData
{
    /// <summary>Exact name of a seeded customer.</summary>
    public required string Customer { get; set; }

    public required string Name { get; set; }

    /// <summary><c>yyyy-MM-dd</c>. Undated Monday groups are set to 2026-01-01 and say so in <see cref="Notes"/>.</summary>
    public required string EffectiveFrom { get; set; }

    /// <summary>A <c>FuelAdjustmentMethod</c> name.</summary>
    public required string FuelMethod { get; set; }

    /// <summary>The quotation's terms under the rate table; see <c>RateAgreementTerms</c>.</summary>
    public int? IncludedDrops { get; set; }

    public ExtraDropChargeData[] ExtraDropCharges { get; set; } = [];
    public decimal? FoulTripPercent { get; set; }
    public decimal? BackloadPercent { get; set; }
    public decimal? DemurrageFreeHours { get; set; }
    public decimal? DemurrageHourlyRate { get; set; }
    public decimal? OtherLocationCharge { get; set; }
    public bool? TollsAndParkingAtCost { get; set; }

    /// <summary>Off unless set: SAS bills no extra drops today.</summary>
    public bool BillExtraDrops { get; set; }

    public bool RatesIncludeVat { get; set; }
    public string? Notes { get; set; }
    public RateLineData[] Lines { get; set; } = [];
}

/// <summary>An extra-drop charge; <see cref="Tonnage"/> is a <c>TruckTonnageClass</c> name, or null for any size.</summary>
public record ExtraDropChargeData
{
    public string? Tonnage { get; set; }
    public decimal Amount { get; set; }
}
