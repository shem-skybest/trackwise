namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// What kind of ship a vessel is. Coarse on purpose: the list exists so a steward can filter and
/// so a wrong entry stands out, not to reproduce a classification society's register.
/// </summary>
public enum VesselType
{
    ContainerShip = 0,
    BulkCarrier = 1,
    Tanker = 2,

    /// <summary>Roll-on/roll-off, which is how vehicle transport moves.</summary>
    RoRo = 3,

    GeneralCargo = 4,

    /// <summary>Unpowered, moved by a tug. Common on the domestic inter-island legs.</summary>
    Barge = 5,

    /// <summary>Short-sea ship feeding a hub port from the smaller ones around it.</summary>
    Feeder = 6,

    Reefer = 7
}
