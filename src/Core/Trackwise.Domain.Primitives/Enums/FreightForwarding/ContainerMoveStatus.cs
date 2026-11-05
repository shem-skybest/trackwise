namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Where a container on a forwarding workfile is on this move, worked out from the vessel's
/// ETD/ETA and the trip events of the truck legs that carry it - never typed by hand.
/// </summary>
public enum ContainerMoveStatus
{
    /// <summary>Nothing has happened to it yet on this move.</summary>
    NotStarted = 0,

    /// <summary>A truck is at the leg's pick-up end.</summary>
    Loading = 1,

    /// <summary>On a truck between the leg's two ends.</summary>
    OnTheRoad = 2,

    /// <summary>A truck is at the leg's drop end.</summary>
    Unloading = 3,

    /// <summary>At a port: dropped by a truck, or arrived by vessel.</summary>
    AtPort = 4,

    /// <summary>Sailed and not yet arrived.</summary>
    OnVessel = 5,

    /// <summary>Dropped at a yard or depot that isn't the client's door.</summary>
    AtYard = 6,

    /// <summary>Dropped at the client's door by a delivery leg.</summary>
    Delivered = 7
}
