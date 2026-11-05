using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Represents a single priced line on a Quotation, pointing at a live TariffRate.
/// </summary>
public class QuotationLine : AuditableEntity, ITenantEntity
{
    public Guid QuotationId { get; private set; }
    public Guid TariffRateId { get; private set; }
    public virtual TariffRate? TariffRate { get; private set; }

    /// <summary>
    /// FIX #2: persisted charge category (previously the constructor argument was dropped).
    /// Lets validation and discount handling work without loading TariffRate/ChargeCode.
    /// </summary>
    public ChargeCategory Category { get; private set; }

    /// <summary>FIX #4: the quotation's currency; Price() converts into this.</summary>
    public string QuotationCurrency { get; private set; } = default!;

    // Operational Classification for Cold Chain Validation
    public OperationalServiceType ServiceType { get; private set; }

    public decimal Quantity { get; private set; }

    /// <summary>
    /// Explicit valuation base for percentage-based lines (e.g. an override of CIF value).
    /// Wins over the quotation-derived base. Must be in the quotation currency.
    /// </summary>
    public decimal? BaseAmount { get; private set; }

    public decimal? FxRate { get; private set; }

    public MarkupType MarkupType { get; private set; }
    public decimal MarkupValue { get; private set; }

    // Price Snapshots (persisted once quote is Issued to prevent drift)
    public decimal? SnapshotBuyingAmount { get; private set; }
    public decimal? SnapshotSellingAmount { get; private set; }
    public string? Currency { get; private set; }

    public string? Notes { get; private set; }
    public virtual Quotation? Quotation { get; private set; }

    protected QuotationLine() { }

    internal static QuotationLine Create(
        Guid quotationId,
        string quotationCurrency,
        TariffRate tariffRate,
        ChargeCategory category,
        OperationalServiceType serviceType,
        decimal quantity,
        MarkupType markupType,
        decimal markupValue,
        decimal? fxRate,
        string? notes,
        decimal? baseAmount = null)
    {
        ArgumentNullException.ThrowIfNull(tariffRate);

        var line = Create(
            quotationId, quotationCurrency, tariffRate.Id, category, serviceType,
            quantity, markupType, markupValue, fxRate, notes, baseAmount);

        line.TariffRate = tariffRate;
        return line;
    }

    internal static QuotationLine Create(
        Guid quotationId,
        string quotationCurrency,
        Guid tariffRateId,
        ChargeCategory category,
        OperationalServiceType serviceType,
        decimal quantity,
        MarkupType markupType,
        decimal markupValue,
        decimal? fxRate,
        string? notes,
        decimal? baseAmount = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quotationCurrency);

        var line = new QuotationLine
        {
            QuotationId = quotationId,
            QuotationCurrency = quotationCurrency.Trim().ToUpperInvariant(),
            TariffRateId = tariffRateId,
            Category = category,
            ServiceType = serviceType,
            Quantity = quantity,
            BaseAmount = baseAmount,
            MarkupType = markupType,
            MarkupValue = markupValue,
            FxRate = fxRate,
            Notes = notes
        };

        line.DomainEvents.Add(new QuotationLineAdded(line.Id, quotationId, tariffRateId));
        return line;
    }

    /// <param name="dynamicValuationBase">
    /// Base for Percentage rates, in the quotation currency. Normally resolved by
    /// <see cref="Quotation.PriceLine"/>; an explicit line BaseAmount takes precedence.
    /// </param>
    public (Money Buying, Money Selling) Price(decimal? dynamicValuationBase = null)
    {
        if (SnapshotBuyingAmount.HasValue && SnapshotSellingAmount.HasValue && Currency is not null)
        {
            return (
                new Money { Amount = SnapshotBuyingAmount.Value, Currency = Currency },
                new Money { Amount = SnapshotSellingAmount.Value, Currency = Currency }
            );
        }

        if (TariffRate is null)
        {
            throw new InvalidOperationException(
                $"TariffRate must be Include()'d before pricing quotation line '{Id}'.");
        }

        var valuation = BaseAmount ?? dynamicValuationBase;
        var buying = TariffRate.Apply(Quantity, FxRate, valuation, QuotationCurrency);

        var sellingAmount = MarkupType switch
        {
            MarkupType.Percentage => buying.Amount * (1 + MarkupValue / 100m),
            MarkupType.Flat => buying.Amount + MarkupValue,
            _ => buying.Amount
        };

        // Uses the persisted Category, not TariffRate.ChargeCode?.Category.
        // A discount reduces BOTH sides; inverting only selling made totals and margin wrong.
        var buyingAmount = buying.Amount;
        if (Category == ChargeCategory.Discount)
        {
            if (buyingAmount > 0) buyingAmount = -buyingAmount;
            if (sellingAmount > 0) sellingAmount = -sellingAmount;
        }

        return (
            new Money { Amount = buyingAmount, Currency = buying.Currency },
            new Money { Amount = sellingAmount, Currency = buying.Currency });
    }

    internal void SnapshotPrices(decimal? dynamicValuationBase = null)
    {
        var (buying, selling) = Price(dynamicValuationBase);
        SnapshotBuyingAmount = buying.Amount;
        SnapshotSellingAmount = selling.Amount;
        Currency = buying.Currency;
    }
}

public record QuotationLineAdded(Guid Id, Guid QuotationId, Guid TariffRateId) : IDomainEvent;
