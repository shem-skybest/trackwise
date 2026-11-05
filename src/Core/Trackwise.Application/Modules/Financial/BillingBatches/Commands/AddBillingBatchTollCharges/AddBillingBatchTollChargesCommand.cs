using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

/// <summary>
/// Bills the tolls, parking and passways the crews paid from the operational fund on this batch's
/// trips, at cost and non-VAT - each receipt once, and none the office disallowed.
/// </summary>
public class AddBillingBatchTollChargesCommand : ICommand<Result<BillingBatchDto>>
{
    public Guid Id { get; set; }
}
