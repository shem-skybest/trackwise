namespace Trackwise.Domain.Primitives.Enums;

/// <summary>One thing the crew reports during a trip, in the order a trip runs.</summary>
public enum TripEventKind
{
    GarageDeparted,
    Arrived,
    HandlingStarted,
    HandlingFinished,
    Departed,
    GarageArrived
}
