using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums.Safety;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Safety.Queries;

public class GetDriverBehaviorEventsQuery : SearchableQuery, IQuery<PagedResult<DriverBehaviorEventDto>>
{
    public Guid? DriverId { get; set; }
    public Guid? TruckId { get; set; }
    public DriverBehaviorEventType? EventType { get; set; }
    public bool? IsReviewed { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
