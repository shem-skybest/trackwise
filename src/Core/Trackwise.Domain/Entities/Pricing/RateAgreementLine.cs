using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// One row of a rate card: the truck-trip rate to a place (a town, or a whole province) or for a
/// distance band, for one truck size.
/// <para>
/// A line is <b>either</b> a distance band (<see cref="MinKm"/>..<see cref="MaxKm"/>, PFI's card)
/// <b>or</b> a place (<see cref="City"/> and/or <see cref="Province"/>, everyone else's). A place
/// line with only a province covers every town in it, and a town line beats it.
/// </para>
/// </summary>
public class RateAgreementLine : Entity, ITenantEntity
{
    public Guid RateAgreementId { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>The row as the client's card writes it, e.g. "SAN JUAN (NON SDO)", "41 - 60 KM".</summary>
    public string Label { get; private set; } = default!;

    /// <summary>The truck size this rate is for, or null when the card has one rate for any size.</summary>
    public TruckTonnageClass? Tonnage { get; private set; }

    /// <summary>The base truck-trip rate, before fuel and extra drops.</summary>
    public decimal Amount { get; private set; }

    public int? MinKm { get; private set; }
    public int? MaxKm { get; private set; }

    public string? City { get; private set; }
    public string? Province { get; private set; }

    /// <summary>The card's own distance for a place row (Bayview lists one). Reference only.</summary>
    public double? DistanceKm { get; private set; }

    /// <summary>Fuel on top as a percentage of <see cref="Amount"/>, for a percentage agreement.</summary>
    public decimal? FuelPercent { get; private set; }

    /// <summary>Fuel on top per diesel price tier, for a diesel-tier agreement.</summary>
    public virtual List<RateFuelSurcharge> FuelSurcharges { get; private set; } = [];

    public bool IsDistanceBand => MinKm.HasValue || MaxKm.HasValue;

    internal static RateAgreementLine Create(Guid agreementId, int sortOrder, RateLineSpec spec)
    {
        var isBand = spec.MinKm.HasValue || spec.MaxKm.HasValue;
        var isPlace = !string.IsNullOrWhiteSpace(spec.City) || !string.IsNullOrWhiteSpace(spec.Province);

        if (isBand == isPlace)
        {
            throw new InvalidOperationException(
                $"Rate line '{spec.Label}' must be either a distance band or a place, not {(isBand ? "both" : "neither")}.");
        }

        if (isBand && (spec.MinKm is not { } min || spec.MaxKm is not { } max || min < 0 || max < min))
        {
            throw new InvalidOperationException($"Rate line '{spec.Label}' needs a distance band from a lower to a higher km.");
        }

        if (spec.Amount <= 0)
        {
            throw new InvalidOperationException($"Rate line '{spec.Label}' needs a rate above zero.");
        }

        var line = new RateAgreementLine
        {
            RateAgreementId = agreementId,
            SortOrder = sortOrder,
            Label = spec.Label.Trim(),
            Tonnage = spec.Tonnage,
            Amount = spec.Amount,
            MinKm = spec.MinKm,
            MaxKm = spec.MaxKm,
            City = Blank(spec.City),
            Province = Blank(spec.Province),
            DistanceKm = spec.DistanceKm,
            FuelPercent = spec.FuelPercent
        };

        foreach (var (dieselPrice, amount) in spec.FuelSurcharges ?? [])
        {
            if (line.FuelSurcharges.Any(s => s.DieselPrice == dieselPrice))
            {
                throw new InvalidOperationException($"Rate line '{spec.Label}' has two surcharges for diesel at {dieselPrice}.");
            }

            line.FuelSurcharges.Add(new RateFuelSurcharge
            {
                RateAgreementLineId = line.Id,
                DieselPrice = dieselPrice,
                Amount = amount
            });
        }

        return line;
    }

    internal RateLineSpec ToSpec() => new(
        Label, Tonnage, Amount, MinKm, MaxKm, City, Province, DistanceKm, FuelPercent,
        FuelSurcharges.OrderBy(s => s.DieselPrice).Select(s => (s.DieselPrice, s.Amount)).ToList());

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
