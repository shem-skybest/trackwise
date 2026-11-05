using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

/// <summary>Checks the batch - every trip completed and priced on its current details - and freezes its charges.</summary>
public class LockBillingBatchCommand : ICommand<Result<BillingBatchDto>>
{
    public Guid Id { get; set; }
}
