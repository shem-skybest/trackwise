using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Queries;

public class GetBillingBatchByIdQuery : IQuery<Result<BillingBatchDto>>
{
    public Guid Id { get; set; }
}
