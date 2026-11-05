using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

public record BillingBatchChargeDto
{
    public Guid Id { get; set; }
    public BillingChargeKind Kind { get; set; }
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }
    public bool IsVatable { get; set; }
    public Guid? TripId { get; set; }
    public long? TripNumber { get; set; }
    public Guid? CashFundExpenseId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
