using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

/// <summary>Drops a batch that won't be billed and frees its trips. Its reference stays used.</summary>
public class CancelBillingBatchCommand : ICommand<Result<BillingBatchDto>>
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = null!;
}
