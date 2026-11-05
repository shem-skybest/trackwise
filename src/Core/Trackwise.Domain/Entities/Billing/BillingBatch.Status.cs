using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Draft -> Locked -> Forwarded -> Billed, with Cancelled from anything not billed. Locking is the
/// lock nSFS's "Send to Billing" put on charges: after it, the trip lines no longer follow their trips.
/// </summary>
public partial class BillingBatch
{
    public BillingBatchStatus Status { get; private set; } = BillingBatchStatus.Draft;

    public string? LockedBy { get; private set; }
    public DateTime? LockedAt { get; private set; }

    /// <summary>Why it was last unlocked; kept after it is locked again.</summary>
    public string? UnlockReason { get; private set; }

    public string? UnlockedBy { get; private set; }
    public DateTime? UnlockedAt { get; private set; }

    public DateOnly? ForwardedOn { get; private set; }
    public string? ForwardedBy { get; private set; }

    /// <summary>SAS's numbers for it, e.g. "BI4907 / BS7259".</summary>
    public string? InvoiceNumber { get; private set; }

    public DateOnly? BilledOn { get; private set; }
    public string? BilledBy { get; private set; }

    public string? CancelReason { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// Checks the batch and freezes it: the lines are brought up to date with <paramref name="trips" />
    /// one last time, then every trip must be completed and priced on its current details.
    /// </summary>
    public void Lock(IEnumerable<BillableTrip> trips, string lockedBy, DateTime at)
    {
        SyncTrips(trips, includeNew: false);

        if (Trips.Count == 0)
        {
            throw new InvalidOperationException("There are no trips on this batch to bill.");
        }

        var problems = Trips
            .Where(t => t.Problem is not null)
            .OrderBy(t => t.BillingDate)
            .Select(t => $"Trip {t.TripNumber}: {t.Problem}")
            .ToList();

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", problems));
        }

        Status = BillingBatchStatus.Locked;
        LockedBy = lockedBy;
        LockedAt = at;
    }

    /// <summary>Back to a draft to fix something, before Accounting has billed it.</summary>
    public void Unlock(string reason, string unlockedBy, DateTime at)
    {
        if (Status is not (BillingBatchStatus.Locked or BillingBatchStatus.Forwarded))
        {
            throw new InvalidOperationException("Only a locked or forwarded batch can be unlocked.");
        }

        Status = BillingBatchStatus.Draft;
        UnlockReason = Required(reason, "Say why the batch is being unlocked.");
        UnlockedBy = unlockedBy;
        UnlockedAt = at;
        LockedBy = null;
        LockedAt = null;
        ForwardedOn = null;
        ForwardedBy = null;
    }

    public void Forward(DateOnly on, string forwardedBy)
    {
        if (Status != BillingBatchStatus.Locked)
        {
            throw new InvalidOperationException("Lock the batch before forwarding it to Accounting.");
        }

        Status = BillingBatchStatus.Forwarded;
        ForwardedOn = on;
        ForwardedBy = forwardedBy;
    }

    public void MarkBilled(string invoiceNumber, DateOnly on, string billedBy)
    {
        if (Status != BillingBatchStatus.Forwarded)
        {
            throw new InvalidOperationException("Only a batch forwarded to Accounting can be marked billed.");
        }

        Status = BillingBatchStatus.Billed;
        InvoiceNumber = Required(invoiceNumber, "Enter the invoice number Accounting billed it under.");
        BilledOn = on;
        BilledBy = billedBy;
    }

    /// <summary>
    /// Drops the batch and frees its trips for another one. The reference stays used. Returns the
    /// trip lines and charges, which the caller deletes.
    /// </summary>
    public (IReadOnlyList<BillingBatchTrip> Trips, IReadOnlyList<BillingBatchCharge> Charges) Cancel(
        string reason, string cancelledBy, DateTime at)
    {
        if (Status is BillingBatchStatus.Billed or BillingBatchStatus.Cancelled)
        {
            throw new InvalidOperationException($"A {Status.ToString().ToLowerInvariant()} batch can't be cancelled.");
        }

        CancelReason = Required(reason, "Say why the batch is being cancelled.");
        CancelledBy = cancelledBy;
        CancelledAt = at;
        Status = BillingBatchStatus.Cancelled;

        var removed = (Trips.ToList(), Charges.ToList());
        Trips.Clear();
        Charges.Clear();
        Recalculate();
        return removed;
    }

    private static string Required(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException(message) : value.Trim();
}
