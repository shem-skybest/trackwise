using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>One fund expense. Truck, trip and payer names are resolved by the handler in batched lookups.</summary>
public record CashFundExpenseDto
{
    public Guid Id { get; set; }
    public DateOnly SpentOn { get; set; }
    public CashExpenseCategory Category { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public string? ReceiptNumber { get; set; }
    public Guid? TruckId { get; set; }
    public string? TruckNumber { get; set; }
    public Guid? TripId { get; set; }
    public long? TripNumber { get; set; }
    public Guid? SpentById { get; set; }
    public string? SpentByName { get; set; }
    public CashExpenseStatus Status { get; set; }
    public DateOnly? ForwardedOn { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? DisallowedReason { get; set; }
}
