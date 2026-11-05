using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums.Safety;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Queries;

/// <summary>Search matches the summary, the place and the case number.</summary>
public class GetIncidentsQuery : SearchableQuery, IQuery<PagedResult<IncidentDto>>
{
    public IncidentKind? Kind { get; set; }
    public IncidentStatus? Status { get; set; }
    public IncidentSeverity? Severity { get; set; }
    public Guid? TripId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? TruckId { get; set; }
    public Guid? DriverId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
