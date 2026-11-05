namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// What kind of trailer this is. Coarse on purpose, matching <c>VesselType</c>'s intent: the list
/// exists so a steward can filter and a wrong entry stands out, not to reproduce a manufacturer's
/// spec sheet.
/// </summary>
public enum TrailerType
{
    DryVan = 0,
    Reefer = 1,
    Flatbed = 2,
    LowBoy = 3,
    Tanker = 4,

    /// <summary>A chassis that carries an intermodal container rather than its own fixed body.</summary>
    ContainerChassis = 5,

    Other = 6
}
