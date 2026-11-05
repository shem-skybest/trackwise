using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>A route's costed rate, with the assumptions it was worked out on.</summary>
public record RouteCostingDto
{
    public TruckTonnageClass TruckSize { get; set; }
    public decimal DieselPricePerLitre { get; set; }

    /// <summary>The day the diesel price on file took effect; null when the caller gave the price.</summary>
    public DateOnly? DieselPriceFrom { get; set; }

    public decimal KmPerLitre { get; set; }
    public decimal DriverDayRate { get; set; }
    public decimal HelperDayRate { get; set; }
    public decimal OtherCrewCost { get; set; }
    public decimal DriverAllowance { get; set; }
    public decimal HelperAllowance { get; set; }
    public decimal AdminPercent { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal Buffer { get; set; }
    public decimal RoundUpTo { get; set; }

    public decimal TotalKm { get; set; }
    public decimal Litres { get; set; }
    public decimal Fuel { get; set; }
    public decimal Crew { get; set; }
    public decimal Expenses { get; set; }
    public decimal Admin { get; set; }
    public decimal Markup { get; set; }
    public decimal BeforeRounding { get; set; }
    public decimal SellingRate { get; set; }
}
