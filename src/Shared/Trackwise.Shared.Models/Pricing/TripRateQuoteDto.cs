namespace Trackwise.Shared.Models;

/// <summary>What a trip costs under its client's agreement, before or after it is applied.</summary>
public record TripRateQuoteDto
{
    public Guid TripId { get; set; }
    public Guid AgreementId { get; set; }
    public required string AgreementName { get; set; }

    /// <summary>The trip date the agreement and diesel price were read for.</summary>
    public DateOnly RatedOn { get; set; }

    public decimal? DieselPrice { get; set; }
    public required string LineLabel { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal FuelAmount { get; set; }
    public required string FuelBasis { get; set; }
    public int ExtraDrops { get; set; }
    public decimal ExtraDropAmount { get; set; }

    /// <summary>Set when nothing was delivered; the base and fuel are already this share of the rate.</summary>
    public decimal? FoulTripPercent { get; set; }

    public decimal Total { get; set; }
    public required string Currency { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = [];
}
