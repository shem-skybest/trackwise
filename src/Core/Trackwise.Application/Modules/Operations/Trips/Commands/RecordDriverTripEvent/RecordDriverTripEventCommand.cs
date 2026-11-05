using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>
/// One tap from the crew's Driver App: arrived, loading or unloading started or finished, departed,
/// or left or got back to the garage. It writes the same trip fields as the office's crew report and
/// goes through the same rules; whoever records an event first keeps it. A pickup's event covers its
/// whole warehouse visit.
/// </summary>
public class RecordDriverTripEventCommand : ICommand
{
    public Guid TripId { get; set; }

    /// <summary>The stop, for a stop event; null for a garage event.</summary>
    public Guid? StopId { get; set; }

    public TripEventKind Kind { get; set; }

    /// <summary>When it happened (with a timezone), if not now. At most a couple of hours back.</summary>
    public DateTime? At { get; set; }

    /// <summary>Reefer temperature (°C), on arrival or departure.</summary>
    public decimal? Temperature { get; set; }

    /// <summary>A drop's outcome, on its departure.</summary>
    public LoadStatus? Outcome { get; set; }

    public int? ReturnedBoxes { get; set; }
    public int? ReturnedBags { get; set; }
    public DropReturnReason? ReturnReason { get; set; }
    public string? OutcomeNote { get; set; }
}
