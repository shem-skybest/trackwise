using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

/// <summary>Takes a trip off a draft, with its charges - e.g. to bill it next period.</summary>
public class RemoveBillingBatchTripCommand : ICommand<Result<BillingBatchDto>>
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
}
