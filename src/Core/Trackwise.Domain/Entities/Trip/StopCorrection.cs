using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// What a stop's recorded event should say instead - the whole recorded state, not only the fields
/// that changed. Times are reported times in UTC. Departure fields apply only to a stop whose
/// departure is recorded; a correction never records an event for the first time.
/// </summary>
/// <param name="ArrivedAt">Required: every corrected stop has an arrival on record.</param>
/// <param name="Outcome">A drop's final outcome; null for a pickup.</param>
public sealed record StopCorrection(
    DateTime ArrivedAt,
    decimal? TemperatureAtArrival = null,
    DateTime? DepartedAt = null,
    decimal? TemperatureAtDeparture = null,
    DateTime? HandlingStartedAt = null,
    DateTime? HandlingFinishedAt = null,
    LoadStatus? Outcome = null,
    int? ReturnedBoxes = null,
    int? ReturnedBags = null,
    DropReturnReason? ReturnReason = null,
    string? OutcomeNote = null);
