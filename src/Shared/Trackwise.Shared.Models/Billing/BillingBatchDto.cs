using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>A client's billing period. <see cref="Trips" /> and <see cref="Charges" /> are empty on list reads.</summary>
public record BillingBatchDto
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string Currency { get; set; } = null!;
    public string? Notes { get; set; }
    public BillingBatchStatus Status { get; set; }
    public int TripCount { get; set; }
    public int DropCount { get; set; }
    public decimal VatableTotal { get; set; }
    public decimal NonVatTotal { get; set; }
    public decimal Total { get; set; }
    public string? LockedBy { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? UnlockReason { get; set; }
    public string? UnlockedBy { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public DateOnly? ForwardedOn { get; set; }
    public string? ForwardedBy { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateOnly? BilledOn { get; set; }
    public string? BilledBy { get; set; }
    public string? CancelReason { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public IReadOnlyList<BillingBatchTripDto> Trips { get; set; } = [];
    public IReadOnlyList<BillingBatchChargeDto> Charges { get; set; } = [];
}
