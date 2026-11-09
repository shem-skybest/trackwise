using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

public class GetQuotationsQuery : IQuery<PagedResult<QuotationSummaryDto>>
{
    public Guid? CustomerId { get; set; }
    public Guid? BranchId { get; set; }
    public FreightMode? FreightMode { get; set; }
    public QuotationStatus? Status { get; set; }
    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
