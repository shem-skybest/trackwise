using System.Text.Json;
using System.Text.Json.Serialization;
using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Represents an individual rate definition within a tariff.
/// </summary>
public class TariffRate : AuditableEntity, ITenantEntity
{
    public Guid TariffId { get; set; }
    public Guid ChargeCodeId { get; set; }
    public virtual ChargeCode? ChargeCode { get; set; }

    public PricingModel PricingModel { get; set; }

    public decimal BaseAmount { get; set; }
    public string Unit { get; set; } = string.Empty; // per kg, per cbm, per container, etc.
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }

    // Demurrage / Detention / Storage dwell rules
    public int? FreeDays { get; set; }
    public decimal? RateAfterFreeDays { get; set; }

    // Percentage pricing rules
    public decimal? Percentage { get; set; }    // e.g., 2.5 for 2.5%
    public string? PercentageBase { get; set; } // "BAS" (base freight buying), "CIF" (declared cargo value), or custom (needs explicit line BaseAmount)

    // Serialized tiers: [{"min":0,"rate":5},{"min":101,"rate":4},{"min":501,"rate":3}]
    public string? BreakpointJson { get; set; }

    /// <summary>FIX #5: AllUnits (default, previous behavior) or Incremental (marginal brackets).</summary>
    public BreakpointMode BreakpointMode { get; set; } = BreakpointMode.AllUnits;

    public virtual Tariff? Tariff { get; set; }

    /// <summary>
    /// The currency <see cref="BaseAmount"/> is in: the charge code's when it names one, otherwise the
    /// tariff's. Charge codes are created without a currency, so reading only the code priced a PHP
    /// tariff's rates as dollars.
    /// </summary>
    public string SourceCurrency =>
        (ChargeCode?.Currency is { Length: > 0 } code ? code : Tariff?.Currency ?? "USD").Trim().ToUpperInvariant();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Applies the PricingModel to determine the buying amount, respects Min/Max, and converts currency.
    /// </summary>
    /// <param name="quantity">Billable units (weight, volume, count, or dwell days).</param>
    /// <param name="fxRate">
    /// Multiplier from <see cref="SourceCurrency"/> to <paramref name="targetCurrency"/>.
    /// Required when the currencies differ; ignored when they match.
    /// </param>
    /// <param name="valuationBase">
    /// Monetary base for Percentage rates, ALREADY expressed in <paramref name="targetCurrency"/>.
    /// </param>
    /// <param name="targetCurrency">Currency of the returned Money (usually the quotation currency).</param>
    public Money Apply(
        decimal quantity,
        decimal? fxRate = null,
        decimal? valuationBase = null,
        string? targetCurrency = null)
    {
        var sourceCurrency = SourceCurrency;
        var currency = string.IsNullOrWhiteSpace(targetCurrency)
            ? sourceCurrency
            : targetCurrency.Trim().ToUpperInvariant();

        // FIX #4: the returned Money is labeled with the currency the amount is actually in.
        decimal factor;
        if (!string.Equals(sourceCurrency, currency, StringComparison.Ordinal))
        {
            if (fxRate is not > 0m)
                throw new InvalidOperationException(
                    $"Rate '{ChargeCode?.Code ?? Id.ToString()}' is in {sourceCurrency} but the target is {currency}; a positive fxRate is required.");
            factor = fxRate.Value;
        }
        else
        {
            // A same-currency override is meaningless, and lines saved with one must still price.
            factor = 1m;
        }

        decimal amount = PricingModel switch
        {
            PricingModel.Flat => BaseAmount * factor,
            PricingModel.PerUnit => CalculatePerUnit(quantity) * factor,
            PricingModel.Breakpoint => CalculateBreakpoint(quantity) * factor,
            // Percentage: valuationBase is already in the target currency, so no FX here.
            PricingModel.Percentage => CalculatePercentage(valuationBase * quantity),
            _ => BaseAmount * factor
        };

        // Min/Max are defined in the charge currency, so convert them before clamping.
        if (Minimum.HasValue) amount = Math.Max(amount, Minimum.Value * factor);
        if (Maximum.HasValue) amount = Math.Min(amount, Maximum.Value * factor);

        return new Money { Amount = amount, Currency = currency };
    }

    private decimal CalculatePerUnit(decimal quantity)
    {
        if (FreeDays.HasValue)
        {
            var billable = Math.Max(0m, quantity - FreeDays.Value);
            return billable * (RateAfterFreeDays ?? BaseAmount);
        }

        return BaseAmount * quantity;
    }

    private decimal CalculateBreakpoint(decimal quantity)
    {
        if (string.IsNullOrWhiteSpace(BreakpointJson))
            return BaseAmount * quantity;

        List<BreakpointTier>? parsed;
        try
        {
            parsed = ParseBreakpointTiers(BreakpointJson);
        }
        catch (JsonException ex)
        {
            // Previously swallowed and silently priced at BaseAmount × qty. Bad tier data must be loud.
            throw new InvalidOperationException(
                $"Rate '{ChargeCode?.Code ?? Id.ToString()}' has invalid BreakpointJson.", ex);
        }

        if (parsed is null || parsed.Count == 0)
            return BaseAmount * quantity;

        var tiers = parsed.OrderBy(t => t.Min).ToList();

        return BreakpointMode == BreakpointMode.Incremental
            ? PriceIncremental(tiers, quantity)
            : PriceAllUnits(tiers, quantity);
    }

    /// <summary>
    /// Highest tier whose Min ≤ quantity prices every unit. Uses Min only, so gaps such as
    /// max 44 / next min 45 can't drop a 44.5 kg shipment into a fallback.
    /// </summary>
    private decimal PriceAllUnits(List<BreakpointTier> tiers, decimal quantity)
    {
        var matched = tiers.LastOrDefault(t => t.Min <= quantity);
        return matched is null ? BaseAmount * quantity : matched.Rate * quantity;
    }

    /// <summary>
    /// Marginal brackets. Each tier runs from its Min up to the next tier's Min (Max is ignored),
    /// and the first tier starts at 0 so every unit is priced exactly once.
    /// </summary>
    private static decimal PriceIncremental(List<BreakpointTier> tiers, decimal quantity)
    {
        decimal total = 0m;

        for (var i = 0; i < tiers.Count; i++)
        {
            var lower = i == 0 ? 0m : tiers[i].Min;
            var upper = i + 1 < tiers.Count ? tiers[i + 1].Min : decimal.MaxValue;

            var units = Math.Max(0m, Math.Min(quantity, upper) - lower);
            total += units * tiers[i].Rate;
        }

        return total;
    }

    /// <summary>
    /// FIX #1: no more silent fallback to quantity/BaseAmount. A percentage without a base is an error.
    /// </summary>
    private decimal CalculatePercentage(decimal? valuationBase)
    {
        if (valuationBase is null)
            throw new InvalidOperationException(
                $"Percentage rate '{ChargeCode?.Code ?? Id.ToString()}' needs a valuation base ({PercentageBase ?? "none configured"}).");

        return valuationBase.Value * ((Percentage ?? 0m) / 100m);
    }

    private static List<BreakpointTier>? ParseBreakpointTiers(string json)
    {
        var trimmed = json.Trim();

        if (trimmed.StartsWith('['))
            return JsonSerializer.Deserialize<List<BreakpointTier>>(trimmed, JsonOptions);

        return JsonSerializer.Deserialize<BreakpointPayload>(trimmed, JsonOptions)?.Tiers;
    }
}

public class BreakpointPayload
{
    [JsonPropertyName("tiers")]
    public List<BreakpointTier> Tiers { get; set; } = new();
}

public class BreakpointTier
{
    [JsonPropertyName("min")]
    public decimal Min { get; set; }

    /// <summary>Informational. Pricing uses Min boundaries only.</summary>
    [JsonPropertyName("max")]
    public decimal? Max { get; set; }

    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }
}
