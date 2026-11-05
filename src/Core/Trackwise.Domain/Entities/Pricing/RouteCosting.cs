using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// What a Cold Chain route should be quoted at, the way Cold Chain's Monday costing boards
/// (2 / 4 TONNER COSTING, CA - 2 / 4 TONNER COSTING) work it out: fuel for the round trip plus
/// the crew's day, 30% admin on top, the size's markup on that, then a buffer, rounded up.
/// <para>
/// The defaults are the boards' formulas with the 2026-10-08 ops meeting's additions: the boards'
/// 3 km/L for a 2-tonner and 2 km/L for a 4-tonner, at the current diesel price rather than the
/// boards' fixed ₱75 / ₱100; a driver's allowance by seniority (₱150 junior on either size; ₱300 on
/// a 2-tonner and ₱450 on a 4-tonner after a year of service); a 60% markup on a 4-tonner; a chargeable ₱1,000 buffer added after the markup, then
/// rounded up to the next ₱100. Every one is an input, so a confirmed change needs no code.
/// </para>
/// </summary>
public static class RouteCosting
{
    public static RouteCostingAssumptions DefaultsFor(TruckTonnageClass size, bool seniorDriver = true) => size switch
    {
        TruckTonnageClass.FourTonner => new RouteCostingAssumptions(
            KmPerLitre: 2,
            DriverDayRate: 755,
            HelperDayRate: 755,
            OtherCrewCost: 163,
            DriverAllowance: seniorDriver ? 450 : 150,
            HelperAllowance: 450,
            AdminPercent: 30,
            MarkupPercent: 60,
            Buffer: 1000,
            RoundUpTo: 100),
        _ => new RouteCostingAssumptions(
            KmPerLitre: 3,
            DriverDayRate: 755,
            HelperDayRate: 755,
            OtherCrewCost: 163,
            DriverAllowance: seniorDriver ? 300 : 150,
            HelperAllowance: 300,
            AdminPercent: 30,
            MarkupPercent: 30,
            Buffer: 1000,
            RoundUpTo: 100)
    };

    /// <param name="garageToWarehouseKm">The three legs of the costing board's round trip.</param>
    public static RouteCostingResult Compute(
        double garageToWarehouseKm,
        double warehouseToDropKm,
        double dropToGarageKm,
        decimal dieselPricePerLitre,
        RouteCostingAssumptions assumptions)
    {
        if (garageToWarehouseKm < 0 || warehouseToDropKm < 0 || dropToGarageKm < 0)
        {
            throw new InvalidOperationException("A leg can't be negative.");
        }

        if (assumptions.KmPerLitre <= 0)
        {
            throw new InvalidOperationException("Km per litre must be above zero.");
        }

        if (assumptions.RoundUpTo < 0)
        {
            throw new InvalidOperationException("Round up to a positive amount, or zero for no rounding.");
        }

        var totalKm = (decimal)(garageToWarehouseKm + warehouseToDropKm + dropToGarageKm);
        var litres = totalKm / assumptions.KmPerLitre;
        var fuel = Math.Round(litres * dieselPricePerLitre, 2);
        var crew = assumptions.DriverDayRate + assumptions.HelperDayRate + assumptions.OtherCrewCost
            + assumptions.DriverAllowance + assumptions.HelperAllowance;
        var expenses = fuel + crew;
        var admin = Math.Round(expenses * assumptions.AdminPercent / 100m, 2);
        var markup = Math.Round((expenses + admin) * assumptions.MarkupPercent / 100m, 2);
        var beforeRounding = expenses + admin + markup + assumptions.Buffer;
        var sellingRate = assumptions.RoundUpTo > 0
            ? Math.Ceiling(beforeRounding / assumptions.RoundUpTo) * assumptions.RoundUpTo
            : beforeRounding;

        return new RouteCostingResult(totalKm, Math.Round(litres, 2), fuel, crew, expenses, admin, markup, beforeRounding, sellingRate);
    }
}

/// <summary>The costing board's constants for one truck size; see <see cref="RouteCosting"/>.</summary>
public sealed record RouteCostingAssumptions(
    decimal KmPerLitre,
    decimal DriverDayRate,
    decimal HelperDayRate,
    decimal OtherCrewCost,
    decimal DriverAllowance,
    decimal HelperAllowance,
    decimal AdminPercent,
    decimal MarkupPercent,
    decimal Buffer,
    decimal RoundUpTo);

/// <summary>Each step of the costing, as the board's columns show it.</summary>
public sealed record RouteCostingResult(
    decimal TotalKm,
    decimal Litres,
    decimal Fuel,
    decimal Crew,
    decimal Expenses,
    decimal Admin,
    decimal Markup,
    decimal BeforeRounding,
    decimal SellingRate);
