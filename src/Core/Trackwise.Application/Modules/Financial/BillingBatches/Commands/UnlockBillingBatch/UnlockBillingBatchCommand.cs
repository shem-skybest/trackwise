using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

/// <summary>Back to a draft to fix something before Accounting bills it. Needs a reason.</summary>
public class UnlockBillingBatchCommand : ICommand<Result<BillingBatchDto>>
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = null!;
}
