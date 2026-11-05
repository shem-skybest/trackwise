using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

public class GetQuotationsQuery : IQuery<PagedResult<QuotationSummaryDto>>
{
    public Guid? CustomerId { get; set; }
    public QuotationStatus? Status { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
