using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

public class DeleteBillingBatchChargeCommand : ICommand<Result<BillingBatchDto>>
{
    public Guid Id { get; set; }
    public Guid ChargeId { get; set; }
}
