using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

/// <summary>
/// Opens a client's billing batch for a period with every completed trip in it that isn't on another
/// batch, and gives it the next SLC-RFV reference.
/// </summary>
public class CreateBillingBatchCommand : ICommand<Result<BillingBatchDto>>
{
    public Guid CustomerId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    /// <summary>Only these trips, e.g. one trip for a client billed per trip. Empty takes the whole period.</summary>
    public List<Guid> TripIds { get; set; } = [];

    public string? Notes { get; set; }
}
