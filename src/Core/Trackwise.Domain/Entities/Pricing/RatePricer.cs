using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Prices a truck-trip from a rate agreement. Pure: the caller loads the agreement, the diesel price
/// in force and the drops, so the rules are testable without a database.
/// <para>
/// The rules, where Cold Chain's cards leave them implicit (confirm with Cold Chain):
/// the trip is charged the rate of its <b>costliest drop</b>; the rate covers
/// <see cref="RateAgreement.IncludedDrops"/> distinct drops in stop order, and each further one is
/// charged <see cref="RateAgreement.ExtraDropChargeFor"/> the truck's size; a distance band is read
/// from the load's own route distance (warehouse to drop); the diesel tier is the highest one at or
/// below the diesel price in force on the trip date. When <b>no</b> drop was delivered (every one a
/// foul trip or cancelled by the client on the day), the trip is charged
/// <see cref="RateAgreement.FoulTripPercent"/> of its rate and fuel, with no extra drops. A card whose
/// rates include VAT is billed net of it, and an agreement that doesn't bill extra drops still
/// counts them, at no charge.
/// </para>
/// </summary>
public static class RatePricer
{
    /// <summary>The Philippine VAT a VAT-inclusive card's amounts are divided out of.</summary>
    public const decimal VatRate = 0.12m;

    public static TripRateQuote Price(
        RateAgreement agreement,
        TruckTonnageClass? tonnage,
        decimal? dieselPrice,
        IReadOnlyList<RatedDrop> drops)
    {
        if (drops.Count == 0)
        {
            throw new InvalidOperationException("The trip has no drops to price.");
        }

        var warnings = new List<string>();
        var priced = drops
            .Select(drop => (Drop: drop, Line: FindLine(agreement, tonnage, drop)))
            .ToList();

        var unmatched = priced.Where(p => p.Line is null).Select(p => Describe(p.Drop)).ToList();
        if (unmatched.Count > 0)
        {
            throw new InvalidOperationException(
                $"'{agreement.Name}' has no rate for {string.Join("; ", unmatched)}"
                + (tonnage is null ? " (the truck has no size set)." : $" on a {SizeName(tonnage.Value)} truck."));
        }

        var rated = priced
            .Select(p => (p.Drop, Line: p.Line!, Fuel: Fuel(agreement, p.Line!, dieselPrice)))
            .OrderByDescending(p => p.Line.Amount + p.Fuel.Amount)
            .First();

        if (agreement.FuelMethod is FuelAdjustmentMethod.DieselTierSurcharge && dieselPrice is null)
        {
            warnings.Add("No diesel price is set, so no fuel was added. Set the current diesel price.");
        }

        var baseAmount = Net(agreement, rated.Line.Amount);
        var fuelAmount = Net(agreement, rated.Fuel.Amount);
        var fuelBasis = agreement.RatesIncludeVat ? $"{rated.Fuel.Basis}; card rates include VAT, billed net" : rated.Fuel.Basis;
        decimal? foulTripPercent = null;
        var nothingDelivered = drops.All(d => d.Refused);

        if (nothingDelivered && agreement.FoulTripPercent is { } percent)
        {
            foulTripPercent = percent;
            baseAmount = Math.Round(baseAmount * percent / 100m, 2);
            fuelAmount = Math.Round(fuelAmount * percent / 100m, 2);
        }
        else if (nothingDelivered)
        {
            warnings.Add("No drop was delivered, but the agreement has no foul-trip charge, so the full rate applies.");
        }

        var otherDropKeys = drops
            .Select(d => d.DropKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(key => !string.Equals(key, rated.Drop.DropKey, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // The rated drop is one of the included drops; the next ones in stop order are the others.
        var extraDropKeys = nothingDelivered
            ? []
            : otherDropKeys.Skip((agreement.IncludedDrops ?? 1) - 1).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var extraCharge = agreement.ExtraDropChargeFor(tonnage) is { } charge ? Net(agreement, charge) : (decimal?)null;
        if (extraDropKeys.Count > 0 && !agreement.BillExtraDrops)
        {
            warnings.Add($"{extraDropKeys.Count} drop(s) past the {agreement.IncludedDrops ?? 1} the rate covers, not charged: "
                + "the agreement doesn't bill extra drops.");
            extraCharge = 0;
        }
        else if (extraDropKeys.Count > 0 && extraCharge is null)
        {
            warnings.Add($"{extraDropKeys.Count} extra drop(s), but the agreement has no extra-drop charge"
                + (tonnage is null ? "." : $" for a {SizeName(tonnage.Value)}."));
        }

        // The rate sits on the load that set it; each extra drop carries its own charge, and an
        // included drop or a repeat load to a place already charged carries nothing - so the loads
        // still sum to the trip price.
        var charges = new Dictionary<Guid, decimal> { [rated.Drop.LoadId] = baseAmount + fuelAmount };
        var charged = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rated.Drop.DropKey };

        foreach (var drop in drops.Where(d => d.LoadId != rated.Drop.LoadId))
        {
            charges[drop.LoadId] = charged.Add(drop.DropKey) && extraDropKeys.Contains(drop.DropKey)
                ? extraCharge ?? 0
                : 0;
        }

        return new TripRateQuote(
            agreement.Id,
            agreement.Name,
            rated.Drop.LoadId,
            rated.Line.Label,
            baseAmount,
            fuelAmount,
            fuelBasis,
            extraDropKeys.Count,
            (extraCharge ?? 0) * extraDropKeys.Count,
            charges,
            warnings,
            foulTripPercent);
    }

    /// <summary>
    /// The line for one drop: a town line beats a province line, and a line for the truck's size
    /// beats a line for any size.
    /// </summary>
    public static RateAgreementLine? FindLine(RateAgreement agreement, TruckTonnageClass? tonnage, RatedDrop drop)
    {
        var sized = agreement.Lines.Where(l => l.Tonnage == tonnage && tonnage is not null).ToList();
        var anySize = agreement.Lines.Where(l => l.Tonnage is null).ToList();

        return Match(sized, drop) ?? Match(anySize, drop);
    }

    /// <summary>
    /// Canonical form for comparing a card's town or province with an address: "Quezon City",
    /// "QUEZON CITY" and "City of Quezon" agree, as do "Biñan" and "BINAN", "Sta. Rosa" and
    /// "Santa Rosa".
    /// </summary>
    public static string NormalizePlace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var text = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var plain = new StringBuilder(text.Length);
        foreach (var c in text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        {
            plain.Append(c);
        }

        var result = plain.ToString();
        result = Regex.Replace(result, @"\bsta\.?\s", "santa ");
        result = Regex.Replace(result, @"\bsto\.?\s", "santo ");
        result = Regex.Replace(result, @"^city of\s+", "");
        result = Regex.Replace(result, @"\s+city$", "");
        result = Regex.Replace(result, @"[^a-z0-9]+", " ");
        return result.Trim();
    }

    private static RateAgreementLine? Match(List<RateAgreementLine> lines, RatedDrop drop)
    {
        if (lines.Count == 0)
        {
            return null;
        }

        var city = NormalizePlace(drop.City);
        var province = NormalizePlace(drop.Province);

        if (city.Length > 0)
        {
            var townLine = lines.FirstOrDefault(l =>
                !l.IsDistanceBand
                && NormalizePlace(l.City) == city
                && (l.Province is null || province.Length == 0 || NormalizePlace(l.Province) == province));

            if (townLine is not null)
            {
                return townLine;
            }
        }

        if (province.Length > 0)
        {
            var provinceLine = lines.FirstOrDefault(l =>
                !l.IsDistanceBand && l.City is null && NormalizePlace(l.Province) == province);

            if (provinceLine is not null)
            {
                return provinceLine;
            }
        }

        var bands = lines.Where(l => l.IsDistanceBand).OrderBy(l => l.MinKm).ToList();
        if (bands.Count == 0 || drop.DistanceKm is not { } km)
        {
            return null;
        }

        // A trip shorter than the first band's floor is still that band: nobody runs a truck for less.
        var roundedKm = (int)Math.Ceiling(km);
        return bands.FirstOrDefault(b => roundedKm >= b.MinKm && roundedKm <= b.MaxKm)
            ?? (roundedKm < bands[0].MinKm ? bands[0] : null);
    }

    private static (decimal Amount, string Basis) Fuel(RateAgreement agreement, RateAgreementLine line, decimal? dieselPrice)
    {
        switch (agreement.FuelMethod)
        {
            case FuelAdjustmentMethod.Percentage when line.FuelPercent is { } percent:
                return (Math.Round(line.Amount * percent / 100m, 2), $"{percent:0.##}% fuel adjustment");

            case FuelAdjustmentMethod.DieselTierSurcharge when dieselPrice is { } price:
                var tier = line.FuelSurcharges
                    .Where(s => s.DieselPrice <= price)
                    .OrderByDescending(s => s.DieselPrice)
                    .FirstOrDefault();

                return tier is null
                    ? (0, $"diesel ₱{price:0.00} is below the lowest tier")
                    : (tier.Amount, $"diesel ₱{price:0.00}, ₱{tier.DieselPrice:0.00} tier");

            default:
                return (0, "no fuel adjustment");
        }
    }

    private static decimal Net(RateAgreement agreement, decimal amount) =>
        agreement.RatesIncludeVat ? Math.Round(amount / (1 + VatRate), 2) : amount;

    private static string Describe(RatedDrop drop)
    {
        var place = string.Join(", ", new[] { drop.City, drop.Province }.Where(p => !string.IsNullOrWhiteSpace(p)));
        return drop.DistanceKm is { } km ? $"{place} ({km:0} km)" : place;
    }

    public static string SizeName(TruckTonnageClass tonnage) => tonnage switch
    {
        TruckTonnageClass.TwoTonner => "2-tonner",
        TruckTonnageClass.ThreeTonner => "3-tonner",
        TruckTonnageClass.FourTonner => "4-tonner",
        _ => "10-tonner"
    };
}
