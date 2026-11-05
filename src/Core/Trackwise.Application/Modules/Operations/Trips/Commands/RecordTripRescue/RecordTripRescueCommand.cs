using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>
/// Another truck takes over drops the trip's own truck can't finish. The drops stay on the trip; the
/// rescue records who delivered them.
/// </summary>
public class RecordTripRescueCommand : ICommand<Result<TripRescueDto>>
{
    public Guid TripId { get; set; }
    public Guid TruckId { get; set; }
    public Guid DriverId { get; set; }
    public Guid? HelperId { get; set; }
    public Guid? SecondHelperId { get; set; }

    /// <summary>The unvisited drops the rescue truck took over.</summary>
    public List<Guid> StopIds { get; set; } = [];

    /// <summary>When the goods were moved across, with a timezone.</summary>
    public DateTime TransferredAt { get; set; }

    public string? Place { get; set; }
    public string Reason { get; set; } = "";

    /// <summary>The incident on this trip that called for the rescue, if one is on record.</summary>
    public Guid? IncidentId { get; set; }
}
