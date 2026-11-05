using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

public class MarkStopDepartedCommand : ICommand
{
    public Guid TripId { get; set; }
    public Guid StopId { get; set; }

    /// <summary>
    /// The drop's real outcome - <see cref="LoadStatus.Delivered"/>,
    /// <see cref="LoadStatus.FoulTrip"/>, <see cref="LoadStatus.CancelledByClient"/>, or
    /// <see cref="LoadStatus.Cancelled"/>. Required for a drop-off; omit it for a pickup.
    /// </summary>
    public LoadStatus? Outcome { get; set; }

    /// <summary>Reefer temperature (°C) at departure, if captured.</summary>
    public decimal? TemperatureAtDeparture { get; set; }

    /// <summary>When the truck actually left (with a timezone), if not now - e.g. retyped from a driver's message.</summary>
    public DateTime? DepartedAt { get; set; }

    /// <summary>When loading (pickup) or unloading (drop) started, with a timezone.</summary>
    public DateTime? HandlingStartedAt { get; set; }

    /// <summary>When loading or unloading finished, with a timezone.</summary>
    public DateTime? HandlingFinishedAt { get; set; }

    /// <summary>Boxes that came back on the truck - a partial delivery. Drops only.</summary>
    public int? ReturnedBoxes { get; set; }

    /// <summary>Bags that came back on the truck. Drops only.</summary>
    public int? ReturnedBags { get; set; }

    /// <summary>Why anything came back; required when anything did.</summary>
    public DropReturnReason? ReturnReason { get; set; }

    /// <summary>What happened at the drop. Required for a foul trip or a client cancellation.</summary>
    public string? OutcomeNote { get; set; }

    /// <summary>
    /// For a pickup: also record departure for the trip's other pickups at the same warehouse that
    /// have arrived but not departed - one visit, one entry.
    /// </summary>
    public bool IncludeSameVisitPickups { get; set; }
}
