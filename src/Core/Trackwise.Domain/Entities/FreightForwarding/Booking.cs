using System.Collections.ObjectModel;
using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

// ── Booking number ───────────────────────────────────────────────────────────

/// <summary>
/// Derives the booking number from the quotation reference so the two documents are
/// visibly linked: QT-2026-00042 -> BK-2026-00042. One booking per quotation, so the
/// result is unique whenever the quotation reference is unique (per tenant).
/// </summary>
public static class BookingNumbering
{
    public const string Prefix = "BK";

    public static string FromQuotationReference(string quotationReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quotationReference);

        var reference = quotationReference.Trim().ToUpperInvariant();

        // Swap the leading alphabetic prefix (QT-, QUO-, Q-...) for BK-
        var match = System.Text.RegularExpressions.Regex.Match(reference, @"^[A-Z]+-");
        return match.Success
            ? Prefix + "-" + reference[match.Length..]
            : Prefix + "-" + reference;   // no recognizable prefix: BK-<reference>
    }
}

/// <summary>
/// A confirmed-intent shipment order. Created as a Draft from an Accepted Quotation,
/// carries a locked copy of the quoted charges, and is the source of truth for
/// both Trips (operations) and Billing (invoicing).
/// </summary>
public partial class Booking : AuditableEntity, ITenantEntity
{
    // Header
    public string BookingNumber { get; private set; } = default!;
    public BookingStatus Status { get; private set; }
    public Guid CustomerId { get; private set; }
    public virtual Customer? Customer { get; private set; }
    public string Currency { get; private set; } = default!;
    public string? CustomerReference { get; private set; }   // customer PO / shipper ref
    public string? Notes { get; private set; }

    // Traceability to the quotation (unique index in EF config)
    public Guid QuotationId { get; private set; }
    public virtual Quotation Quotation { get; set; }
    public string QuotationReference { get; private set; } = default!;

    // Logistics context (snapshotted from quotation, editable while Draft)
    public TransportMode Mode { get; private set; }
    public FreightMode FreightMode { get; private set; }
    public ShipmentType ShipmentType { get; private set; }
    public Incoterm Incoterm { get; private set; }
    public LaneTransit Lane { get; private set; } = default!;
    public DateTimeOffset? RequestedPickupStart => Lane.FirstPickup.WindowStart;
    public DateTimeOffset? RequestedDeliveryEnd => Lane.FinalDrop.WindowEnd;
    public CargoSpecification? CargoSpec { get; private set; }
    public decimal? DeclaredCargoValuePhp { get; private set; }

    // Specialized handling
    public TemperatureControl? TemperatureSpec { get; private set; }
    public bool IsTemperatureControlled => TemperatureSpec is not null;
    public bool IsHazardous { get; private set; }
    public bool RequiresCustomsBrokerage { get; private set; }

    // Billing terms (used by the Billing module when generating the invoice)
    public int PaymentTermsDays { get; private set; }
    public decimal? DepositRequiredPercent { get; private set; }

    // Credit & approval
    public CreditCheckStatus CreditCheckStatus { get; private set; }
    public string? CreditCheckedBy { get; private set; }
    public DateTimeOffset? CreditCheckedAt { get; private set; }
    public string? CreditCheckNotes { get; private set; }

    // Lifecycle
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }

    private readonly List<BookingChargeLine> _charges = [];
    public virtual IReadOnlyCollection<BookingChargeLine> Charges => new ReadOnlyCollection<BookingChargeLine>(_charges);

    private readonly List<BookingDocument> _documents = [];
    public virtual IReadOnlyCollection<BookingDocument> Documents => new ReadOnlyCollection<BookingDocument>(_documents);

    protected Booking() { }

    // ── Creation from quotation ──────────────────────────────────────────────

    /// <summary>
    /// Builds a Draft booking from an Accepted quotation. Quoted charges are copied from the
    /// quotation's issued price snapshots, so later tariff/FX changes cannot alter the booking.
    /// Quotation.Lines must be loaded.
    /// </summary>
    public static Booking CreateDraftFromQuotation(
        Quotation quotation,
        int paymentTermsDays = 30,
        bool customerCreditCheckRequired = false)
    {
        ArgumentNullException.ThrowIfNull(quotation);

        if (quotation.Status != QuotationStatus.Accepted)
            throw new InvalidOperationException("A booking can only be created from an Accepted quotation.");

        var booking = new Booking
        {
            BookingNumber = BookingNumbering.FromQuotationReference(quotation.ReferenceNumber),
            Status = BookingStatus.Draft,
            CustomerId = quotation.CustomerId,
            Currency = quotation.Currency,
            QuotationId = quotation.Id,
            QuotationReference = quotation.ReferenceNumber,
            Mode = quotation.Mode,
            FreightMode = quotation.FreightMode,
            ShipmentType = quotation.ShipmentType,
            Incoterm = quotation.Incoterm,
            Lane = quotation.Lane,
            CargoSpec = quotation.CargoSpec,
            DeclaredCargoValuePhp = quotation.DeclaredCargoValuePhp,
            TemperatureSpec = quotation.TemperatureSpec,
            IsHazardous = quotation.IsHazardous,
            RequiresCustomsBrokerage = quotation.RequiresCustomsBrokerage,
            PaymentTermsDays = paymentTermsDays,
            CreditCheckStatus =  customerCreditCheckRequired ? CreditCheckStatus.Pending : CreditCheckStatus.NotRequired,
            Notes = quotation.Notes
        };

        foreach (var line in quotation.Lines)
            booking._charges.Add(BookingChargeLine.FromQuotationLine(booking.Id, line));

        //booking.GenerateDocumentRequirements();

        booking.DomainEvents.Add(new BookingDraftCreated(booking.Id, quotation.Id, quotation.CustomerId));
        return booking;
    }

    // ── Header / cargo edits (Draft only) ────────────────────────────────────

    public void UpdateHeader(string? customerReference, string? notes, int paymentTermsDays, decimal? depositRequiredPercent)
    {
        EnsureDraft();

        if (paymentTermsDays < 0)
            throw new ArgumentOutOfRangeException(nameof(paymentTermsDays));
        if (depositRequiredPercent is < 0m or > 100m)
            throw new ArgumentOutOfRangeException(nameof(depositRequiredPercent), "Deposit must be between 0 and 100.");

        CustomerReference = customerReference?.Trim();
        Notes = notes;
        PaymentTermsDays = paymentTermsDays;
        DepositRequiredPercent = depositRequiredPercent;
    }

    /// <summary>Operational dates/stops can be refined on the booking (e.g. confirmed pickup window).</summary>
    public void UpdateLane(LaneTransit lane)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(lane);
        Lane = lane;
        DomainEvents.Add(new BookingUpdated(Id));
    }

    /// <summary>
    /// Moves the requested pickup/delivery windows after the booking is Confirmed.
    /// The route itself (stops, order, locations) must stay the same; only the windows may change.
    /// Raises BookingRescheduled so Trips can update its plan and Billing can react to pricing impact.
    /// </summary>
    public void Reschedule(LaneTransit newLane, string reason, string requestedBy)
    {
        if (Status is not (BookingStatus.Confirmed or BookingStatus.InProgress))
            throw new InvalidOperationException(
                "Reschedule applies to Confirmed or InProgress bookings. Use UpdateLane while Draft.");

        ArgumentNullException.ThrowIfNull(newLane);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBy);

        EnsureSameRoute(Lane, newLane);

        var oldPickup = RequestedPickupStart;
        var oldDelivery = RequestedDeliveryEnd;

        // Any window change on any stop counts, not just the headline dates
        var windowsChanged = !Lane.Stops.OrderBy(s => s.Sequence)
            .Select(s => (s.WindowStart, s.WindowEnd))
            .SequenceEqual(newLane.Stops.OrderBy(s => s.Sequence).Select(s => (s.WindowStart, s.WindowEnd)));

        if (!windowsChanged) return;   // nothing to reschedule

        Lane = newLane;

        DomainEvents.Add(new BookingRescheduled(
            Id, oldPickup, RequestedPickupStart, oldDelivery, RequestedDeliveryEnd, reason, requestedBy));
    }

    private static void EnsureSameRoute(LaneTransit current, LaneTransit proposed)
    {
        var a = current.Stops.OrderBy(s => s.Sequence).ToList();
        var b = proposed.Stops.OrderBy(s => s.Sequence).ToList();

        var same = a.Count == b.Count &&
                   a.Zip(b).All(p => p.First.LocationId == p.Second.LocationId &&
                                     p.First.Type == p.Second.Type &&
                                     p.First.Sequence == p.Second.Sequence);

        if (!same)
            throw new InvalidOperationException(
                "Reschedule cannot change the route. Stops, order, and locations must remain the same.");
    }

    /// <summary>
    /// Cargo may be refined (actual weight, pallet count) but its declared value is tied to the quote.
    /// </summary>
    public void UpdateCargo(CargoSpecification cargoSpec)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(cargoSpec);
        CargoSpec = cargoSpec;
        DomainEvents.Add(new BookingUpdated(Id));
    }

    // ── Charges ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a charge that was not in the quotation (e.g. detention, extra handling).
    /// Allowed until the booking is Completed, so Billing can bill them later.
    /// </summary>
    public BookingChargeLine AddAdditionalCharge(
        ChargeCategory category,
        string description,
        decimal quantity,
        decimal unitBuying,
        decimal unitSelling,
        string reason)
    {
        if (Status is BookingStatus.Completed or BookingStatus.Cancelled)
            throw new InvalidOperationException($"Charges cannot be added to a {Status} booking.");

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var charge = BookingChargeLine.CreateAdditional(Id, category, description, quantity, unitBuying, unitSelling, Currency, reason);
        _charges.Add(charge);
        DomainEvents.Add(new BookingChargeAdded(Id, charge.Id));
        return charge;
    }

    public void RemoveAdditionalCharge(Guid chargeId)
        => RemoveAdditionalCharge(chargeId, allowInvoicedDraft: false);

    public void RemoveAdditionalCharge(Guid chargeId, bool allowInvoicedDraft)
    {
        var charge = _charges.FirstOrDefault(c => c.Id == chargeId);
        if (charge is null) return;

        if (charge.Source == BookingChargeSource.Quoted)
            throw new InvalidOperationException("Quoted charges are locked and cannot be removed.");
        if (charge.BillingStatus == BillingStatus.Invoiced && !allowInvoicedDraft)
            throw new InvalidOperationException("An invoiced charge cannot be removed.");

        _charges.Remove(charge);
        DomainEvents.Add(new BookingChargeRemoved(Id, chargeId));
    }

    public void UpdateAdditionalCharge(
        Guid chargeId,
        string description,
        decimal quantity,
        decimal unitBuying,
        decimal unitSelling,
        string reason,
        bool allowInvoicedDraft)
    {
        var charge = _charges.FirstOrDefault(c => c.Id == chargeId)
            ?? throw new InvalidOperationException($"Charge '{chargeId}' not found on this booking.");

        if (charge.Source == BookingChargeSource.Quoted)
            throw new InvalidOperationException("Quoted charges are locked and cannot be changed.");
        if (charge.BillingStatus == BillingStatus.Invoiced && !allowInvoicedDraft)
            throw new InvalidOperationException("An invoiced charge cannot be changed.");

        charge.Update(description, quantity, unitBuying, unitSelling, reason);
        DomainEvents.Add(new BookingChargeUpdated(Id, chargeId));
    }

    public BookingTotals GetTotals() =>
        new(Currency,
            _charges.Sum(c => c.BuyingAmount),
            _charges.Sum(c => c.SellingAmount));

    public IReadOnlyList<BookingChargeLine> GetUnbilledCharges() =>
        _charges.Where(c => c.BillingStatus == BillingStatus.Unbilled && c.IsBillable).ToList();

    /// <summary>Called by Billing after it creates the invoice, so charges are never billed twice.</summary>
    public void MarkChargesInvoiced(Guid invoiceId, IEnumerable<Guid> chargeIds)
    {
        var ids = chargeIds.ToHashSet();
        foreach (var charge in _charges.Where(c => ids.Contains(c.Id)))
            charge.MarkInvoiced(invoiceId);
    }

    public void ReleaseChargesFromInvoice(Guid invoiceId)
    {
        foreach (var charge in _charges.Where(c => c.InvoiceId == invoiceId))
            charge.MarkUnbilled(invoiceId);
    }

    // ── Documents ────────────────────────────────────────────────────────────

    public BookingDocument AddDocument(BookingDocumentType type, string? reference = null, DateTimeOffset? expiresAt = null)
    {
        EnsureNotClosed();

        var doc = BookingDocument.Create(Id, type, BookingDocumentStatus.Submitted, reference, expiresAt);
        _documents.Add(doc);
        return doc;
    }

    public void VerifyDocument(Guid documentId, string verifiedBy)
    {
        EnsureNotClosed();
        GetDocument(documentId).Verify(verifiedBy);
        DomainEvents.Add(new BookingDocumentVerified(Id, documentId));
    }

    public void RejectDocument(Guid documentId, string rejectedBy, string reason)
    {
        EnsureNotClosed();
        GetDocument(documentId).Reject(rejectedBy, reason);
    }

    public void WaiveDocument(Guid documentId, string waivedBy, string reason)
    {
        EnsureNotClosed();
        GetDocument(documentId).Waive(waivedBy, reason);
    }

    private BookingDocument GetDocument(Guid id) =>
        _documents.FirstOrDefault(d => d.Id == id)
        ?? throw new InvalidOperationException($"Document '{id}' not found on booking '{BookingNumber}'.");

    /// <summary>Derives which documents this shipment needs from its characteristics.</summary>
    private void GenerateDocumentRequirements()
    {
        Require(BookingDocumentType.Kyc);
        Require(BookingDocumentType.CommercialInvoice);
        Require(BookingDocumentType.PackingList);

        if (FreightMode == FreightMode.Import) Require(BookingDocumentType.ImportPermit);
        if (FreightMode == FreightMode.Export) Require(BookingDocumentType.ExportPermit);
        if (IsHazardous) Require(BookingDocumentType.DangerousGoodsDeclaration);
        if (IsTemperatureControlled) Require(BookingDocumentType.ColdChainCertificate);
        if (DeclaredCargoValuePhp > Quotation.HighValueCargoThresholdPhp) Require(BookingDocumentType.InsuranceCertificate);

        void Require(BookingDocumentType type) =>
            _documents.Add(BookingDocument.Create(Id, type, BookingDocumentStatus.Required));
    }

    // ── Credit check ─────────────────────────────────────────────────────────

    public void ApproveCredit(string checkedBy, string? notes = null)
    {
        EnsureNotClosed();
        CreditCheckStatus = CreditCheckStatus.Approved;
        CreditCheckedBy = checkedBy;
        CreditCheckedAt = DateTimeOffset.UtcNow;
        CreditCheckNotes = notes;
    }

    public void RejectCredit(string checkedBy, string reason)
    {
        EnsureNotClosed();
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        CreditCheckStatus = CreditCheckStatus.Rejected;
        CreditCheckedBy = checkedBy;
        CreditCheckedAt = DateTimeOffset.UtcNow;
        CreditCheckNotes = reason;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    /// <summary>Returns the reasons this booking cannot be confirmed yet (empty = ready).</summary>
    public IReadOnlyList<string> GetConfirmationBlockers()
    {
        var blockers = new List<string>();

        if (_charges.Count == 0)
            blockers.Add("Booking has no charges.");

        if (CreditCheckStatus is CreditCheckStatus.Pending)
            blockers.Add("Credit check is pending.");
        else if (CreditCheckStatus is CreditCheckStatus.Rejected)
            blockers.Add("Credit check was rejected.");

        foreach (var d in _documents.Where(d => !d.IsSatisfied))
            blockers.Add($"Document '{d.Type}' is {d.Status}.");

        return blockers;
    }

    /// <summary>
    /// Confirms the booking. Raises BookingConfirmed, which Billing (draft invoice)
    /// and Trips (create trips) subscribe to.
    /// </summary>
    public void Confirm(string confirmedBy)
    {
        if (Status != BookingStatus.Draft)
            throw new InvalidOperationException("Only Draft bookings can be confirmed.");

        var blockers = GetConfirmationBlockers();
        if (blockers.Count > 0)
            throw new InvalidOperationException("Booking cannot be confirmed: " + string.Join(" ", blockers));

        Status = BookingStatus.Confirmed;
        ConfirmedAt = DateTimeOffset.UtcNow;
        ConfirmedBy = confirmedBy;

        DomainEvents.Add(new BookingConfirmed(Id, CustomerId, QuotationId, BookingNumber));
    }

    /// <summary>Called by the Trips module when the first trip is dispatched.</summary>
    public void MarkInProgress()
    {
        if (Status == BookingStatus.InProgress) return;
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Only Confirmed bookings can move to InProgress.");

        Status = BookingStatus.InProgress;
        DomainEvents.Add(new BookingStarted(Id));
    }

    /// <summary>Called by the Trips module when all trips are delivered.</summary>
    public void Complete()
    {
        if (Status != BookingStatus.InProgress)
            throw new InvalidOperationException("Only InProgress bookings can be completed.");

        Status = BookingStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        DomainEvents.Add(new BookingCompleted(Id, CustomerId));
    }

    public void Cancel(string cancelledBy, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status is BookingStatus.Completed or BookingStatus.Cancelled)
            throw new InvalidOperationException($"A {Status} booking cannot be cancelled.");

        if (Status == BookingStatus.InProgress)
            throw new InvalidOperationException("An in-progress booking cannot be cancelled. Cancel its trips first.");

        var wasConfirmed = Status == BookingStatus.Confirmed;

        Status = BookingStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
        CancelledBy = cancelledBy;
        CancellationReason = reason;

        // Billing/Trips only need to react if the booking had already been confirmed.
        DomainEvents.Add(new BookingCancelled(Id, reason, wasConfirmed));
    }

    // ── Guards ───────────────────────────────────────────────────────────────

    private void EnsureDraft()
    {
        if (Status != BookingStatus.Draft)
            throw new InvalidOperationException("This change is only allowed while the booking is in Draft status.");
    }

    private void EnsureNotClosed()
    {
        if (Status is BookingStatus.Completed or BookingStatus.Cancelled)
            throw new InvalidOperationException($"Booking is {Status}.");
    }
}

// ── Child entity: charge line (the Billing handoff) ──────────────────────────

/// <summary>
/// A locked snapshot of a quoted charge, or an additional charge added after booking.
/// Billing reads these as its invoice "Charge lines" (quoted plus extra charges).
/// </summary>
public class BookingChargeLine : AuditableEntity, ITenantEntity
{
    public Guid BookingId { get; private set; }
    public virtual Booking? Booking { get; private set; }

    public BookingChargeSource Source { get; private set; }

    // Traceability (null for additional charges)
    public Guid? QuotationLineId { get; private set; }
    public Guid? TariffRateId { get; private set; }

    public ChargeCategory Category { get; private set; }
    public OperationalServiceType ServiceType { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal Quantity { get; private set; }

    public string Currency { get; private set; } = default!;
    public decimal BuyingAmount { get; private set; }    // cost (for margin / vendor payables)
    public decimal SellingAmount { get; private set; }   // what the customer pays

    public bool IsBillable { get; private set; } = true;
    public string? Reason { get; private set; }          // why an additional charge was added

    public BillingStatus BillingStatus { get; private set; }
    public Guid? InvoiceId { get; private set; }

    protected BookingChargeLine() { }

    internal static BookingChargeLine FromQuotationLine(Guid bookingId, QuotationLine line)
    {
        if (line.SnapshotSellingAmount is null || line.SnapshotBuyingAmount is null || line.Currency is null)
            throw new InvalidOperationException(
                $"Quotation line '{line.Id}' has no issued price snapshot; the quotation must be Issued before booking.");

        return new BookingChargeLine
        {
            BookingId = bookingId,
            Source = BookingChargeSource.Quoted,
            QuotationLineId = line.Id,
            TariffRateId = line.TariffRateId,
            Category = line.Category,
            ServiceType = line.ServiceType,
            Description = line.Notes ?? line.Category.ToString(),
            Quantity = line.Quantity,
            Currency = line.Currency,
            BuyingAmount = line.SnapshotBuyingAmount.Value,
            SellingAmount = line.SnapshotSellingAmount.Value,
            BillingStatus = BillingStatus.Unbilled
        };
    }

    internal static BookingChargeLine CreateAdditional(
        Guid bookingId, ChargeCategory category, string description,
        decimal quantity, decimal unitBuying, decimal unitSelling, string currency, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (quantity <= 0m) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitBuying < 0m || unitSelling < 0m) throw new ArgumentOutOfRangeException(nameof(unitSelling), "Unit amounts cannot be negative.");

        // Discounts reduce both sides, same convention as QuotationLine.Price()
        var sign = category == ChargeCategory.Discount ? -1m : 1m;

        return new BookingChargeLine
        {
            BookingId = bookingId,
            Source = BookingChargeSource.Additional,
            Category = category,
            ServiceType = default,
            Description = description.Trim(),
            Quantity = quantity,
            Currency = currency,
            BuyingAmount = sign * unitBuying * quantity,
            SellingAmount = sign * unitSelling * quantity,
            Reason = reason,
            BillingStatus = BillingStatus.Unbilled
        };
    }

    internal void MarkInvoiced(Guid invoiceId)
    {
        if (BillingStatus == BillingStatus.Invoiced)
            throw new InvalidOperationException($"Charge '{Id}' is already invoiced.");

        BillingStatus = BillingStatus.Invoiced;
        InvoiceId = invoiceId;
    }

    internal void MarkUnbilled(Guid invoiceId)
    {
        if (BillingStatus != BillingStatus.Invoiced || InvoiceId != invoiceId)
            throw new InvalidOperationException($"Charge '{Id}' is not assigned to invoice '{invoiceId}'.");

        BillingStatus = BillingStatus.Unbilled;
        InvoiceId = null;
    }

    internal void Update(string description, decimal quantity, decimal unitBuying, decimal unitSelling, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (quantity <= 0m) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitBuying < 0m || unitSelling < 0m) throw new ArgumentOutOfRangeException(nameof(unitSelling));

        var sign = Category == ChargeCategory.Discount ? -1m : 1m;
        Description = description.Trim();
        Quantity = quantity;
        BuyingAmount = sign * unitBuying * quantity;
        SellingAmount = sign * unitSelling * quantity;
        Reason = reason.Trim();
    }
}

// ── Child entity: document / compliance requirement ──────────────────────────

public class BookingDocument : AuditableEntity, ITenantEntity
{
    public Guid BookingId { get; private set; }
    public BookingDocumentType Type { get; private set; }
    public BookingDocumentStatus Status { get; private set; }
    public string? Reference { get; private set; }        // permit no., KYC ref, file id
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ReviewNotes { get; private set; }

    /// <summary>Verified or waived, and not expired.</summary>
    public bool IsSatisfied =>
        Status is BookingDocumentStatus.Verified or BookingDocumentStatus.Waived &&
        (ExpiresAt is null || ExpiresAt > DateTimeOffset.UtcNow);

    protected BookingDocument() { }

    internal static BookingDocument Create(
        Guid bookingId, BookingDocumentType type, BookingDocumentStatus status,
        string? reference = null, DateTimeOffset? expiresAt = null) =>
        new()
        {
            BookingId = bookingId,
            Type = type,
            Status = status,
            Reference = reference,
            ExpiresAt = expiresAt
        };

    internal void Verify(string by)
    {
        if (Status == BookingDocumentStatus.Required)
            throw new InvalidOperationException($"Document '{Type}' has not been submitted yet.");

        Status = BookingDocumentStatus.Verified;
        ReviewedBy = by;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    internal void Reject(string by, string reason)
    {
        Status = BookingDocumentStatus.Rejected;
        ReviewedBy = by;
        ReviewedAt = DateTimeOffset.UtcNow;
        ReviewNotes = reason;
    }

    internal void Waive(string by, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = BookingDocumentStatus.Waived;
        ReviewedBy = by;
        ReviewedAt = DateTimeOffset.UtcNow;
        ReviewNotes = reason;
    }
}

public record BookingTotals(string Currency, decimal Buying, decimal Selling)
{
    public decimal Margin => Selling - Buying;
    public decimal MarginPercent => Selling == 0m ? 0m : Math.Round(Margin / Selling * 100m, 2);
}

// ── Domain events ────────────────────────────────────────────────────────────

public record BookingDraftCreated(Guid Id, Guid QuotationId, Guid CustomerId) : IDomainEvent;
public record BookingUpdated(Guid Id) : IDomainEvent;
public record BookingChargeAdded(Guid Id, Guid ChargeId) : IDomainEvent;
public record BookingChargeUpdated(Guid Id, Guid ChargeId) : IDomainEvent;
public record BookingChargeRemoved(Guid Id, Guid ChargeId) : IDomainEvent;
public record BookingDocumentVerified(Guid Id, Guid DocumentId) : IDomainEvent;
public record BookingConfirmed(Guid Id, Guid CustomerId, Guid QuotationId, string BookingNumber) : IDomainEvent;
public record BookingStarted(Guid Id) : IDomainEvent;
public record BookingCompleted(Guid Id, Guid CustomerId) : IDomainEvent;
public record BookingCancelled(Guid Id, string Reason, bool WasConfirmed) : IDomainEvent;
public record BookingRescheduled(
    Guid Id,
    DateTimeOffset? OldPickupStart, DateTimeOffset? NewPickupStart,
    DateTimeOffset? OldDeliveryEnd, DateTimeOffset? NewDeliveryEnd,
    string Reason, string RequestedBy) : IDomainEvent;
