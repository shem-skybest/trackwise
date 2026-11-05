using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Safety;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Queries;

[RequiresFeature(TenantFeature.Dvir)]
public class GetDvirReportsQuery : SearchableQuery, IQuery<PagedResult<DvirReportDto>>
{
    public Guid? TruckId { get; set; }
    public Guid? DriverId { get; set; }
    public DvirStatus? Status { get; set; }
    public DvirType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
