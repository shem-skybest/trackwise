using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>
/// Records a whole crew report at once - the office retyping a driver's Messenger message: garage
/// times plus, per stop, arrival, loading/unloading, departure, temperatures and the drop's outcome.
/// All of it is saved or none of it: one bad time names its stop and nothing is written.
/// <para>
/// Stops are applied in trip order. A pickup's entry covers every pickup of the same warehouse visit.
/// Anything already recorded is left as it is, so the same report can be topped up as more of the
/// message comes in.
/// </para>
/// </summary>
public class RecordTripReportCommand : ICommand
{
    public Guid TripId { get; set; }

    /// <summary>When the truck left the garage, with a timezone.</summary>
    public DateTime? GarageDepartedAt { get; set; }

    /// <summary>When the truck got back to the garage, with a timezone.</summary>
    public DateTime? GarageArrivedAt { get; set; }

    public List<TripReportStopEntry> Stops { get; set; } = [];
}
