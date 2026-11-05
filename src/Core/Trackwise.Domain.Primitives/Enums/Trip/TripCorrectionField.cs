namespace Trackwise.Domain.Primitives.Enums;

/// <summary>Which recorded value on a trip a correction changed.</summary>
public enum TripCorrectionField
{
    ArrivedAt,
    TemperatureAtArrival,
    HandlingStartedAt,
    HandlingFinishedAt,
    DepartedAt,
    TemperatureAtDeparture,
    Outcome,
    ReturnedBoxes,
    ReturnedBags,
    ReturnReason,
    OutcomeNote,
    GarageDepartedAt,
    GarageArrivedAt
}
