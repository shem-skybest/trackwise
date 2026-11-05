using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>
/// Corrects a stop's recorded event: the whole recorded state as it should read, plus why. Every
/// time carries a timezone. Departure fields apply only where the departure is already recorded.
/// </summary>
public class CorrectTripStopCommand : ICommand
{
    public Guid TripId { get; set; }
    public Guid StopId { get; set; }

    public DateTime ArrivedAt { get; set; }
    public decimal? TemperatureAtArrival { get; set; }

    public DateTime? HandlingStartedAt { get; set; }
    public DateTime? HandlingFinishedAt { get; set; }
    public DateTime? DepartedAt { get; set; }
    public decimal? TemperatureAtDeparture { get; set; }

    /// <summary>A drop's outcome, required when its departure is recorded; omit for a pickup.</summary>
    public LoadStatus? Outcome { get; set; }

    public int? ReturnedBoxes { get; set; }
    public int? ReturnedBags { get; set; }
    public DropReturnReason? ReturnReason { get; set; }
    public string? OutcomeNote { get; set; }

    /// <summary>Why the recorded values are being changed. Required.</summary>
    public string Reason { get; set; } = "";
}
