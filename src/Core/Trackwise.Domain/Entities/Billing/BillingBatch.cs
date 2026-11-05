using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// One client's billing period, handed to Accounting as one reference - Monday's MONTHLY BILLING
/// UPDATES row and nSFS's per-period <c>SLC-RFV####-26-M</c> job. It collects the client's completed
/// trips up to the cut-off, priced from the rate agreement, plus the charges ops bill case by case.
/// <para>
/// Cold Chain doesn't bill per trip or through a workfile: the batch is the unit. It is prepared as a
/// draft, locked once checked (which freezes the charges), forwarded to Accounting as a printed
/// summary, and marked billed with SAS's invoice number. See <c>BillingBatch.Status.cs</c>.
/// </para>
/// </summary>
public partial class BillingBatch : AuditableEntity, ITenantEntity
{
    private string? notes;

    /// <summary>The <c>SLC-RFV####-26-M</c> reference, from its own counter.</summary>
    public string ReferenceNumber { get; private set; } = null!;

    public Guid CustomerId { get; private set; }
    public virtual Customer Customer { get; private set; } = null!;

    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }

    /// <summary>ISO 4217, the tenant's currency when the batch was opened.</summary>
    public string Currency { get; private set; } = null!;

    public string? Notes
    {
        get => notes;
        set => notes = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public int TripCount { get; private set; }
    public int DropCount { get; private set; }

    /// <summary>The trip rates plus the VATable charges: nSFS's "Service" column.</summary>
    public decimal VatableTotal { get; private set; }

    /// <summary>Charges billed at cost, e.g. tolls and parking: nSFS's "Non-VAT" column.</summary>
    public decimal NonVatTotal { get; private set; }

    public decimal Total => VatableTotal + NonVatTotal;

    public virtual List<BillingBatchTrip> Trips { get; private set; } = [];
    public virtual List<BillingBatchCharge> Charges { get; private set; } = [];

    public BillingPeriod Period => new(PeriodStart, PeriodEnd);

    public static BillingBatch Create(string referenceNumber, Guid customerId, BillingPeriod period, string currency)
    {
        if (period.End < period.Start)
        {
            throw new InvalidOperationException("The period can't end before it starts.");
        }

        return new BillingBatch
        {
            ReferenceNumber = referenceNumber,
            CustomerId = customerId,
            PeriodStart = period.Start,
            PeriodEnd = period.End,
            Currency = currency
        };
    }

    /// <summary>
    /// Brings the trip lines up to date with <paramref name="trips" />, and with
    /// <paramref name="includeNew" /> adds the ones not on the batch yet. Returns the new lines, which
    /// the caller must add to the repository.
    /// </summary>
    public IReadOnlyList<BillingBatchTrip> SyncTrips(IEnumerable<BillableTrip> trips, bool includeNew)
    {
        EnsureDraft();
        LoadLines();
        var added = new List<BillingBatchTrip>();

        foreach (var trip in trips)
        {
            if (Trips.FirstOrDefault(t => t.TripId == trip.TripId) is { } line)
            {
                line.CopyFrom(trip);
            }
            else if (includeNew)
            {
                line = BillingBatchTrip.Create(Id, trip);
                Trips.Add(line);
                added.Add(line);
            }
        }

        Recalculate();
        return added;
    }

    /// <summary>
    /// Takes a trip off the batch, e.g. to bill it next period, with the charges that belong to it.
    /// The caller deletes the rows it returns.
    /// </summary>
    public (BillingBatchTrip Line, IReadOnlyList<BillingBatchCharge> Charges) RemoveTrip(Guid tripId)
    {
        EnsureDraft();
        LoadLines();
        var line = Trips.FirstOrDefault(t => t.TripId == tripId)
            ?? throw new InvalidOperationException("That trip isn't on this batch.");

        var charges = Charges.Where(c => c.TripId == tripId).ToList();
        Trips.Remove(line);
        Charges.RemoveAll(charges.Contains);
        Recalculate();
        return (line, charges);
    }

    public BillingBatchCharge AddCharge(BillingChargeSpec spec)
    {
        EnsureDraft();
        LoadLines();
        if (spec.TripId is { } tripId && Trips.All(t => t.TripId != tripId))
        {
            throw new InvalidOperationException("That trip isn't on this batch.");
        }

        var charge = BillingBatchCharge.Create(Id, spec);
        Charges.Add(charge);
        Recalculate();
        return charge;
    }

    /// <summary>The caller deletes the row it returns.</summary>
    public BillingBatchCharge RemoveCharge(Guid chargeId)
    {
        EnsureDraft();
        LoadLines();
        var charge = Charges.FirstOrDefault(c => c.Id == chargeId)
            ?? throw new InvalidOperationException("That charge isn't on this batch.");

        Charges.Remove(charge);
        Recalculate();
        return charge;
    }

    /// <summary>
    /// Loads both collections before either changes. The totals read both, and lazy-loading one
    /// after a line was added to the other makes EF discover the new line before the caller has
    /// added it to the repository - which the navigation guard refuses.
    /// </summary>
    private void LoadLines()
    {
        _ = Trips.Count;
        _ = Charges.Count;
    }

    private void Recalculate()
    {
        TripCount = Trips.Count;
        DropCount = Trips.Sum(t => t.Drops);
        VatableTotal = Trips.Sum(t => t.Amount ?? 0) + Charges.Where(c => c.IsVatable).Sum(c => c.Amount);
        NonVatTotal = Charges.Where(c => !c.IsVatable).Sum(c => c.Amount);
    }

    private void EnsureDraft()
    {
        if (Status != BillingBatchStatus.Draft)
        {
            throw new InvalidOperationException(Status == BillingBatchStatus.Locked
                ? "This batch is locked. Unlock it to change it."
                : $"This batch is {Status.ToString().ToLowerInvariant()} and can't be changed.");
        }
    }
}
