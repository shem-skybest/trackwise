using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// Read model for a workfile. Carries the per-type rules the entity exposes so the UI can hide the
/// panels a given type does not use without re-deriving them from the shipment type.
/// </summary>
public class WorkfileDto
{
    public Guid Id { get; set; }
    public WorkfileType Type { get; set; }
    public string ReferenceNumber { get; set; } = default!;
    public string ReferenceSeries { get; set; } = default!;

    /// <summary>Set only on Regular files.</summary>
    public FreightMode? Direction { get; set; }

    /// <summary>Set only on Trucking files.</summary>
    public TruckingVariant? Variant { get; set; }

    public string? BillOfLadingNumber { get; set; }
    public Guid? BookingId { get; set; }
    public long? BookingNumber { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;
    public BillIndicator? BillIndicator { get; set; }

    public string? VesselName { get; set; }
    public string? VoyageNumber { get; set; }
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public string? Commodity { get; set; }

    public Guid? ShippingLineId { get; set; }
    public string? ShippingLineName { get; set; }
    public Guid? CoLoaderId { get; set; }
    public string? CoLoaderName { get; set; }
    public Guid? ForwarderId { get; set; }
    public string? ForwarderName { get; set; }
    public Guid? TruckerId { get; set; }
    public string? TruckerName { get; set; }
    public Guid? BrokerId { get; set; }
    public string? BrokerName { get; set; }

    public int? CreditTermDays { get; set; }
    public CreditType? CreditType { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? FinalExchangeRate { get; set; }
    public string? TaggingCategory { get; set; }
    public bool IsAdvanceBilling { get; set; }
    public ReportCategory ReportCategory { get; set; }

    public DateTimeOffset? GarageDepartureAt { get; set; }
    public DateTimeOffset? GarageArrivalAt { get; set; }

    public Guid? GensetId { get; set; }
    public string? GensetName { get; set; }
    public GensetBillingType? GensetBillingType { get; set; }
    public decimal? GensetHourlyRate { get; set; }
    public decimal? GensetHoursUsed { get; set; }
    public decimal? GensetFlatAmount { get; set; }
    public decimal? GensetTotalCharge { get; set; }
    public string? GensetNotes { get; set; }

    public List<WorkfileContainerDto> Containers { get; set; } = [];

    /// <summary>Set only on a truck leg: the forwarding workfile it moves boxes for.</summary>
    public Guid? ParentWorkfileId { get; set; }
    public string? ParentReferenceNumber { get; set; }
    public TruckLegRole? LegRole { get; set; }

    /// <summary>A forwarding workfile's truck legs, oldest first.</summary>
    public List<TruckLegDto> Legs { get; set; } = [];

    /// <summary>A truck leg's loads, one per box it moves.</summary>
    public List<TruckLegLoadDto> LegLoads { get; set; } = [];

    /// <summary>A forwarding workfile's door-to-door steps: vessel dates and its legs' truck events.</summary>
    public List<WorkfileTimelineEntryDto> Timeline { get; set; } = [];

    public WorkfileStatus Status { get; set; }
    public bool IsEditable { get; set; }
    public bool IsForwardedToAccounting { get; set; }
    public DateTimeOffset? ForwardedToAccountingAt { get; set; }
    public string? AccountingRemarks { get; set; }
    public bool IsReferenceLocked { get; set; }

    public string? ClosedBy { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }

    // Per-type capabilities, so the UI does not re-implement the rules in TypeScript.
    public ContainerKind ContainerKind { get; set; }
    public bool RequiresBookingNumber { get; set; }
    public bool RequiresBillIndicator { get; set; }
    public bool RequiresDeliveryLocation { get; set; }
    public bool SupportsGarageTracking { get; set; }
    public bool SupportsBudgetDistribution { get; set; }
    public bool SupportsGenset { get; set; }
    public bool SupportsTruckLegs { get; set; }
    public bool PromptsForVatTreatment { get; set; }
}

/// <summary>Lightweight version of <see cref="WorkfileDto"/> for list/table views.</summary>
public class WorkfileSummaryDto
{
    public Guid Id { get; set; }
    public WorkfileType Type { get; set; }
    public string ReferenceNumber { get; set; } = default!;
    public string? BillOfLadingNumber { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public WorkfileStatus Status { get; set; }
    public bool IsForwardedToAccounting { get; set; }
    public bool IsReferenceLocked { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request body for promoting a confirmed Booking into a workfile. The booking id comes from the
/// route, so the wire shape differs from the command.
/// </summary>
public class PromoteBookingToWorkfileRequest
{
    public WorkfileType Type { get; set; }
    public FreightMode? Direction { get; set; }
    public TruckingVariant? Variant { get; set; }
    public ReportCategory? ReportCategory { get; set; }

    /// <summary>Overrides the allocated reference. Must still be unique.</summary>
    public string? ReferenceNumber { get; set; }

    public string? BillOfLadingNumber { get; set; }
    public BillIndicator? BillIndicator { get; set; }
    public string? VesselName { get; set; }
    public string? VoyageNumber { get; set; }
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public string? Commodity { get; set; }
    public Guid? ShippingLineId { get; set; }
    public Guid? CoLoaderId { get; set; }
    public Guid? ForwarderId { get; set; }
    public Guid? TruckerId { get; set; }
    public Guid? BrokerId { get; set; }
    public int? CreditTermDays { get; set; }
    public CreditType? CreditType { get; set; }
    public decimal? ExchangeRate { get; set; }
    public string? TaggingCategory { get; set; }
    public bool IsAdvanceBilling { get; set; }
}

/// <summary>Request body for cancelling a workfile - the actor comes from the authenticated user.</summary>
public record CancelWorkfileRequest(string Reason);

/// <summary>Request body for an approver's disapproval - the file goes back to ops with a reason.</summary>
public record ReturnWorkfileToOpsRequest(string Reason);

/// <summary>Request body for Accounting's close and for releasing a locked reference.</summary>
public record WorkfileAccountingRemarksRequest(string? Remarks);
