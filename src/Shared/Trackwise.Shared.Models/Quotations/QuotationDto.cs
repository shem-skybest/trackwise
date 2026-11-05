using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models.Quotations;

namespace Trackwise.Shared.Models;

/// <summary>
/// Lightweight version of QuotationDto for list/table views (no lines).
/// </summary>
public class QuotationSummaryDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string ReferenceNumber { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public QuotationStatus Status { get; set; }
    public int LineCount { get; set; }
    public FreightMode FreightMode { get; set; }
    public decimal TotalSellingAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class QuotationDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;
    public string ReferenceNumber { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public QuotationStatus Status { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }

    // Logistics & Transport Attributes
    public TransportMode Mode { get; set; }
    public FreightMode FreightMode { get; set; }
    public ShipmentType ShipmentType { get; set; }
    public Incoterm Incoterm { get; set; }

    // Route & Stops
    public decimal TotalDistanceKm { get; set; }
    public List<RouteStopSummaryDto> Stops { get; set; } = [];

    // Cargo & Cold Chain Profile
    public string? Commodity { get; set; } = default!;
    public decimal? TotalWeightKg { get; set; }
    public int? PalletCount { get; set; }
    public decimal? DeclaredCargoValueAmount { get; set; }
    public string? DeclaredCargoValueCurrency { get; set; }
    public bool RequiresCustomsBrokerage { get; set; }
    public bool IsHazardous { get; set; }
    public TemperatureControlDto? TemperatureSpec { get; set; }

    // Governance & Approvals
    public bool RequiresSpecialApproval { get; set; }
    public string? ApprovalEscalationReason { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    // Financial Totals
    public decimal TotalBuyingAmount { get; set; }
    public decimal TotalSellingAmount { get; set; }
    public decimal NetMarginAmount => TotalSellingAmount - TotalBuyingAmount;
    public decimal NetMarginPercentage => TotalBuyingAmount > 0
        ? Math.Round(((TotalSellingAmount - TotalBuyingAmount) / TotalBuyingAmount) * 100m, 2)
        : 0m;

    public List<QuotationLineDto> Lines { get; set; } = [];
    public string? Notes { get; set; }
}

public record RouteStopSummaryDto(
    int Sequence,
    Guid LocationId,
    string LocationCode,
    string LocationName,
    StopType Type,
    DateTimeOffset? WindowStart,
    DateTimeOffset? WindowEnd);

public class LaneTransitDto
{
    // For Multi-Drop / Milk Runs
    public List<RouteStopDto>? Stops { get; set; }

    // Fallback for Point-to-Point routes
    public Guid? OriginLocationId { get; set; } = null;
    public Guid? DestinationLocationId { get; set; } = null;
    public DateTimeOffset? RequestedPickupWindowStart { get; set; }
    public DateTimeOffset? RequestedDeliveryWindowEnd { get; set; }

    public decimal? TotalDistanceKm { get; set; }
}

public record RouteStopDto(
    int Sequence,
    Guid LocationId,
    StopType Type,
    DateTimeOffset? WindowStart,
    DateTimeOffset? WindowEnd,
    string? Instructions);

public record CargoSpecificationDto(
    string Commodity,
    decimal TotalWeightKg,
    int PalletCount,
    decimal DeclaredCargoValueAmount,
    string DeclaredCargoValueCurrency,
    bool RequiresActiveRefrigeration);

/// <summary>
/// Request body for rejecting a Quotation - the only Quotation write whose wire shape
/// differs from its command (Id comes from the route, RejectedBy from the authenticated user).
/// </summary>
public record RejectQuotationRequest(string Reason);
