using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Queries;

/// <summary>
/// What a route should be quoted at, by Cold Chain's costing board. Each assumption left out takes
/// the truck size's default; the diesel price defaults to the one in force today.
/// </summary>
public class CostRouteQuery : IQuery<Result<RouteCostingDto>>
{
    public TruckTonnageClass TruckSize { get; set; }
    public double GarageToWarehouseKm { get; set; }
    public double WarehouseToDropKm { get; set; }
    public double DropToGarageKm { get; set; }

    /// <summary>A driver under a year of service gets the junior allowance.</summary>
    public bool SeniorDriver { get; set; } = true;

    /// <summary>The client's local date, for the diesel price in force; dates here are UTC otherwise.</summary>
    public DateOnly? On { get; set; }

    public decimal? DieselPricePerLitre { get; set; }
    public decimal? KmPerLitre { get; set; }
    public decimal? DriverDayRate { get; set; }
    public decimal? HelperDayRate { get; set; }
    public decimal? OtherCrewCost { get; set; }
    public decimal? DriverAllowance { get; set; }
    public decimal? HelperAllowance { get; set; }
    public decimal? AdminPercent { get; set; }
    public decimal? MarkupPercent { get; set; }
    public decimal? Buffer { get; set; }
    public decimal? RoundUpTo { get; set; }
}
