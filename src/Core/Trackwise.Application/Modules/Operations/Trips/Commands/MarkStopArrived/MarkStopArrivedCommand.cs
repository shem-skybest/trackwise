using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

public class MarkStopArrivedCommand : ICommand
{
    public Guid TripId { get; set; }
    public Guid StopId { get; set; }

    /// <summary>Reefer temperature (°C) at arrival, if captured.</summary>
    public decimal? TemperatureAtArrival { get; set; }

    /// <summary>When the truck actually arrived (with a timezone), if not now - e.g. retyped from a driver's message.</summary>
    public DateTime? ArrivedAt { get; set; }

    /// <summary>
    /// For a pickup: also mark the trip's other pickups at the same warehouse that haven't arrived
    /// yet - one visit, one entry.
    /// </summary>
    public bool IncludeSameVisitPickups { get; set; }
}
