namespace Trackwise.Domain.Primitives.Enums;

/// <summary>One step on a forwarding workfile's door-to-door timeline.</summary>
public enum WorkfileTimelineEventKind
{
    /// <summary>A truck leg's requested pick-up date, before any truck has arrived.</summary>
    PickupPlanned = 0,

    TruckArrivedAtPickup = 1,
    TruckLeftPickup = 2,
    TruckArrivedAtDrop = 3,
    TruckLeftDrop = 4,

    /// <summary>The vessel's ETD as staff typed it.</summary>
    VesselDeparted = 5,

    /// <summary>The vessel's ETA as staff typed it.</summary>
    VesselArrived = 6
}
