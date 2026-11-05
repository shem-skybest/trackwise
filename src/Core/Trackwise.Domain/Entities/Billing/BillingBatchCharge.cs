using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A charge on a billing batch besides the truck-trip rates: demurrage, a backload, a different
/// delivery location, tolls and parking at cost. VATable charges add to the "Service" column of
/// Accounting's summary, the rest to "Non-VAT".
/// </summary>
public class BillingBatchCharge : AuditableEntity, ITenantEntity
{
    public Guid BillingBatchId { get; private set; }
    public BillingChargeKind Kind { get; private set; }
    public string Description { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public bool IsVatable { get; private set; }

    /// <summary>The trip the charge belongs to, when it belongs to one.</summary>
    public Guid? TripId { get; private set; }

    /// <summary>The crew receipt it was taken from, so the same toll is never billed twice.</summary>
    public Guid? CashFundExpenseId { get; private set; }

    internal static BillingBatchCharge Create(Guid batchId, BillingChargeSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Description))
        {
            throw new InvalidOperationException("Describe the charge.");
        }

        if (spec.Amount <= 0)
        {
            throw new InvalidOperationException("A charge must be more than zero.");
        }

        return new BillingBatchCharge
        {
            BillingBatchId = batchId,
            Kind = spec.Kind,
            Description = spec.Description.Trim(),
            Amount = spec.Amount,
            IsVatable = spec.IsVatable,
            TripId = spec.TripId,
            CashFundExpenseId = spec.CashFundExpenseId
        };
    }
}

public sealed record BillingChargeSpec(
    BillingChargeKind Kind,
    string Description,
    decimal Amount,
    bool IsVatable,
    Guid? TripId = null,
    Guid? CashFundExpenseId = null);
