using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

/// <summary>A charge on top of the trip rates: demurrage, a backload, a different location, tolls at cost.</summary>
public class AddBillingBatchChargeCommand : ICommand<Result<BillingBatchDto>>
{
    public Guid Id { get; set; }
    public BillingChargeKind Kind { get; set; }
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }

    /// <summary>False for charges billed at cost, e.g. tolls and parking.</summary>
    public bool IsVatable { get; set; } = true;

    /// <summary>The batch trip it belongs to, if any.</summary>
    public Guid? TripId { get; set; }
}
