using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

/// <summary>Brings a draft up to date: its trips' latest prices, and the client's trips completed in the period since.</summary>
public class RefreshBillingBatchCommand : ICommand<Result<BillingBatchDto>>
{
    public Guid Id { get; set; }
}
