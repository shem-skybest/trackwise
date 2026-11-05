using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>
/// The crew takes back their own last tap - only for a few minutes and only while nothing was
/// recorded after it. Anything else is the office's to correct.
/// </summary>
public class UndoDriverTripEventCommand : ICommand
{
    public Guid TripId { get; set; }

    /// <summary>The stop, for a stop event; null for a garage event.</summary>
    public Guid? StopId { get; set; }

    public TripEventKind Kind { get; set; }
}
