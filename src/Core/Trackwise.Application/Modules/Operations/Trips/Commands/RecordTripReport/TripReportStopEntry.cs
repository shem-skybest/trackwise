using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>What the report says about one stop. Every time carries a timezone.</summary>
public class TripReportStopEntry
{
    public Guid StopId { get; set; }

    public DateTime? ArrivedAt { get; set; }
    public decimal? TemperatureAtArrival { get; set; }

    /// <summary>Loading (pickup) or unloading (drop) start; saved with the departure.</summary>
    public DateTime? HandlingStartedAt { get; set; }

    /// <summary>Loading or unloading finish; saved with the departure.</summary>
    public DateTime? HandlingFinishedAt { get; set; }

    public DateTime? DepartedAt { get; set; }
    public decimal? TemperatureAtDeparture { get; set; }

    /// <summary>A drop's outcome, required with its departure; omit for a pickup.</summary>
    public LoadStatus? Outcome { get; set; }

    public int? ReturnedBoxes { get; set; }
    public int? ReturnedBags { get; set; }
    public DropReturnReason? ReturnReason { get; set; }
    public string? OutcomeNote { get; set; }
}
