using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Queries;

/// <summary>Batches newest period first, with their totals but not their lines. Search matches the reference or the client.</summary>
public class GetBillingBatchesQuery : SearchableQuery, IQuery<PagedResult<BillingBatchDto>>
{
    public BillingBatchStatus? Status { get; set; }
    public Guid? CustomerId { get; set; }
}
