using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

/// <summary>Records that the printed summary went to Accounting.</summary>
public class ForwardBillingBatchCommand : ICommand<Result<BillingBatchDto>>
{
    public Guid Id { get; set; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? ForwardedOn { get; set; }
}
