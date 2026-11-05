namespace Trackwise.DbMigrator.Models;

/// <summary>One row of a seeded rate card - a place or a distance band.</summary>
public record RateLineData
{
    public required string Label { get; set; }

    /// <summary>A <c>TruckTonnageClass</c> name, or null for one rate for any size.</summary>
    public string? Tonnage { get; set; }

    public decimal Amount { get; set; }
    public int? MinKm { get; set; }
    public int? MaxKm { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public double? DistanceKm { get; set; }
    public decimal? FuelPercent { get; set; }
    public RateSurchargeData[] FuelSurcharges { get; set; } = [];
}
