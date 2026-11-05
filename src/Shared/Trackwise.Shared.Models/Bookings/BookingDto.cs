using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.Enums.Invoice;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;


/// <summary>
/// Read model for a Booking: header, logistics snapshot, locked charges, document checklist and totals.
/// </summary>
public class BookingDto
{
    // Header
    public Guid Id { get; set; }
    public string BookingNumber { get; set; } = default!;
    public BookingStatus Status { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public string? CustomerReference { get; set; }
    public string? Notes { get; set; }

    // Quotation link
    public Guid QuotationId { get; set; }
    public string QuotationReference { get; set; } = default!;

    // Logistics snapshot
    public TransportMode Mode { get; set; }
    public FreightMode FreightMode { get; set; }
    public ShipmentType ShipmentType { get; set; }
    public Incoterm Incoterm { get; set; }
    public DateTimeOffset? RequestedPickupStart { get; set; }
    public DateTimeOffset? RequestedDeliveryEnd { get; set; }
    public decimal TotalDistanceKm { get; set; }
    public List<BookingStopDto> Stops { get; set; } = [];
    public BookingCargoDto? Cargo { get; set; }
    public BookingTemperatureDto? Temperature { get; set; }
    public bool IsHazardous { get; set; }
    public bool RequiresCustomsBrokerage { get; set; }
    public decimal? DeclaredCargoValuePhp { get; set; }

    // Billing terms
    public int PaymentTermsDays { get; set; }
    public decimal? DepositRequiredPercent { get; set; }

    // Credit check
    public CreditCheckStatus CreditCheckStatus { get; set; }
    public string? CreditCheckedBy { get; set; }
    public DateTimeOffset? CreditCheckedAt { get; set; }
    public string? CreditCheckNotes { get; set; }

    // Lifecycle
    public string? ConfirmedBy { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Money (from the locked charge lines)
    public decimal TotalSelling { get; set; }
    public decimal TotalBuying { get; set; }   // internal cost: hide from customer-facing endpoints
    public decimal Margin => TotalSelling - TotalBuying;
    public decimal MarginPercent => TotalSelling == 0m ? 0m : Math.Round(Margin / TotalSelling * 100m, 2);

    public List<BookingChargeDto> Charges { get; set; } = [];
    public List<BookingInvoiceSummaryDto> BillingInvoices { get; set; } = [];
    public List<BookingDocumentDto> Documents { get; set; } = [];

    /// <summary>Why Confirm() would fail right now (empty = ready). Straight from Booking.GetConfirmationBlockers().</summary>
    public List<string> ConfirmationBlockers { get; set; } = [];

    /// <summary>Trips carrying this booking's loads, newest first.</summary>
    public List<BookingTripDto> Trips { get; set; } = [];
}

public class BookingInvoiceSummaryDto
{
    public Guid Id { get; set; }
    public long Number { get; set; }
    public InvoiceStatus Status { get; set; }
    public BookingInvoicePurpose Purpose { get; set; }
    public Guid? RelatedInvoiceId { get; set; }
}


/// <summary>Lightweight version of BookingDto for list/table views. Fully SQL-projectable.</summary>
public class BookingSummaryDto
{
    public Guid Id { get; set; }
    public string BookingNumber { get; set; } = default!;
    public BookingStatus Status { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;
    public string? CustomerReference { get; set; }
    public Guid QuotationId { get; set; }
    public string QuotationReference { get; set; } = default!;
    public string Currency { get; set; } = default!;

    public TransportMode Mode { get; set; }
    public FreightMode FreightMode { get; set; }
    public ShipmentType ShipmentType { get; set; }
    public DateTimeOffset? RequestedPickupStart { get; set; }
    public DateTimeOffset? RequestedDeliveryEnd { get; set; }

    public decimal TotalSelling { get; set; }
    public decimal UnbilledSelling { get; set; }
    public int ChargeCount { get; set; }

    public CreditCheckStatus CreditCheckStatus { get; set; }
    public int OpenDocumentCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public class BookingStopDto
{
    public int Sequence { get; set; }
    public Guid LocationId { get; set; }
    public string? LocationName { get; set; }   // resolved by the detail handler in one batched lookup
    public StopType Type { get; set; }
    public DateTimeOffset? WindowStart { get; set; }
    public DateTimeOffset? WindowEnd { get; set; }
    public string? Instructions { get; set; }
}

public class BookingCargoDto
{
    public string Commodity { get; set; } = default!;
    public decimal TotalWeightKg { get; set; }
    public int PalletCount { get; set; }
    public decimal DeclaredValueAmount { get; set; }
    public string DeclaredValueCurrency { get; set; } = default!;
    public bool RequiresActiveRefrigeration { get; set; }
}

public class BookingTemperatureDto
{
    public TemperatureZone Zone { get; set; }
    public decimal SetpointCelsius { get; set; }
    public decimal AllowableVarianceCelsius { get; set; }
    public ComplianceRegime Compliance { get; set; }
    public bool RequiresContinuousTelematics { get; set; }
}

public class BookingChargeDto
{
    public Guid Id { get; set; }
    public BookingChargeSource Source { get; set; }
    public ChargeCategory Category { get; set; }
    public OperationalServiceType ServiceType { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public string Currency { get; set; } = default!;
    public decimal SellingAmount { get; set; }
    public decimal BuyingAmount { get; set; }   // internal: hide from customer-facing endpoints
    public bool IsBillable { get; set; }
    public string? Reason { get; set; }
    public BillingStatus BillingStatus { get; set; }
    public Guid? InvoiceId { get; set; }
}

/// <summary>Edits a Draft booking. Maps to Booking.UpdateHeader.</summary>
public record UpdateBookingHeaderRequest(
    string? CustomerReference,
    string? Notes,
    int PaymentTermsDays,
    decimal? DepositRequiredPercent);

/// <summary>Only windows can change, never the route. The handler applies these onto the existing lane by Sequence.</summary>
public record BookingStopWindowDto(int Sequence, DateTimeOffset? WindowStart, DateTimeOffset? WindowEnd);

public record RescheduleBookingRequest(List<BookingStopWindowDto> Stops, string Reason);

/// <summary>Maps to Booking.AddAdditionalCharge (detention, extra handling, ...).</summary>
public record AddAdditionalChargeRequest(
    ChargeCategory Category,
    string Description,
    decimal Quantity,
    decimal UnitBuying,
    decimal UnitSelling,
    string Reason);

/// <summary>Maps to Booking.AddDocument; fulfils the open requirement of that type.</summary>
public record AddBookingDocumentRequest(BookingDocumentType Type, string? Reference, DateTimeOffset? ExpiresAt);

//public record CancelBookingRequest(string Reason);
public class BookingDocumentDto
{
    public Guid Id { get; set; }
    public BookingDocumentType Type { get; set; }
    public BookingDocumentStatus Status { get; set; }
    public string? Reference { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsSatisfied { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}

/// <summary>
/// Request body for converting an approved Quotation into a Booking.
/// </summary>
public class ConvertQuotationToBookingRequest
{
    public Guid? ContainerTypeId { get; set; }
    public DateTime? RequestedPickupDate { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request body for confirming a Booking - the wire shape differs from the command
/// (Id comes from the route, ConfirmedBy from the authenticated user).
/// </summary>
public record ConfirmBookingRequest(string? CarrierReference);

/// <summary>
/// Request body for cancelling a Booking - the only Booking write whose wire shape
/// differs from its command (Id comes from the route, CancelledBy from the authenticated user).
/// </summary>
///
//public record CancelBookingRequest(string Reason);
public record CancelBookingRequest(
    string? Reason,
    CancellationParty? CancelledByParty = null,
    BookingLossReason? LossReason = null);


/// <summary>
/// A Confirmed booking with no trip yet, shown on the planning board.
/// Replaces the old Cold Chain shape (rate agreement, call time, pickup site, drops).
/// </summary>
public class UnAssignedBookingDto
{
    public Guid Id { get; set; }
    public string BookingNumber { get; set; } = default!;
    public string QuotationReference { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;

    public TransportMode Mode { get; set; }
    public FreightMode FreightMode { get; set; }
    public ShipmentType ShipmentType { get; set; }

    public DateTimeOffset? RequestedPickupStart { get; set; }
    public DateTimeOffset? RequestedDeliveryEnd { get; set; }

    public string OriginName { get; set; } = default!;
    public string DestinationName { get; set; } = default!;
    public int StopCount { get; set; }

    /// <summary>Names of the Dropoff stops, in route order (multi-drop runs have several).</summary>
    public List<string> DropNames { get; set; } = [];

    public string? Commodity { get; set; }
    public decimal? TotalWeightKg { get; set; }
    public int? PalletCount { get; set; }

    public bool IsTemperatureControlled { get; set; }
    public TemperatureZone? TemperatureZone { get; set; }
    public decimal? SetpointCelsius { get; set; }
    public bool IsHazardous { get; set; }
}
