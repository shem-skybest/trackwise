using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>What a period's fill-ups add up to, overall, per fleet card and per truck.</summary>
public record FuelSummaryDto
{
    public int FillUps { get; set; }
    public decimal Liters { get; set; }
    public decimal Amount { get; set; }

    /// <summary>The amount without the 12% VAT included in pump prices, as Monday's dispatch board showed it.</summary>
    public decimal AmountNetOfVat { get; set; }

    public decimal? AveragePricePerLiter { get; set; }
    public int ForReview { get; set; }
    public int Flagged { get; set; }
    public required string Currency { get; set; }
    public IReadOnlyList<FuelCardTotalDto> ByCard { get; set; } = [];
    public IReadOnlyList<FuelTruckTotalDto> ByTruck { get; set; } = [];
}

/// <summary>One card's fill-ups, to check against that card's statement. No card means paid some other way.</summary>
public record FuelCardTotalDto
{
    public FleetCard? FleetCard { get; set; }
    public int FillUps { get; set; }
    public decimal Liters { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountNetOfVat { get; set; }
}

public record FuelTruckTotalDto
{
    public Guid TruckId { get; set; }
    public string? TruckNumber { get; set; }
    public int FillUps { get; set; }
    public decimal Liters { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Kilometres between the odometer readings that fall in the period.</summary>
    public int? Km { get; set; }

    public decimal? KmPerLiter { get; set; }
}
