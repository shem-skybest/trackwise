using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// The price of one truck-trip under an agreement, broken down the way the rate card and the bill
/// read it: the base rate of the drop that costs most, fuel on top of it, and extra drops.
/// </summary>
/// <param name="FoulTripPercent">
/// Set when nothing was delivered: the base and fuel are already cut to this share of the rate.
/// </param>
public sealed record TripRateQuote(
    Guid AgreementId,
    string AgreementName,
    Guid RatedLoadId,
    string LineLabel,
    decimal BaseAmount,
    decimal FuelAmount,
    string FuelBasis,
    int ExtraDrops,
    decimal ExtraDropAmount,
    IReadOnlyDictionary<Guid, decimal> ChargePerLoad,
    IReadOnlyList<string> Warnings,
    decimal? FoulTripPercent = null)
{
    public decimal Total => BaseAmount + FuelAmount + ExtraDropAmount;
}
