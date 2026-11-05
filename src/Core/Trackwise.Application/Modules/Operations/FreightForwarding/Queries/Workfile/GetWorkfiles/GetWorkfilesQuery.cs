using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

[RequiresFeature(TenantFeature.Workfiles)]
public class GetWorkfilesQuery : SearchableQuery, IQuery<PagedResult<WorkfileSummaryDto>>
{
    public WorkfileType? Type { get; set; }
    public WorkfileStatus? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public bool? IsForwardedToAccounting { get; set; }
}
