namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// What a truck leg does for its forwarding workfile. A label for people and reports - the leg's
/// actual ends are on its loads, so a role never constrains where the truck goes.
/// </summary>
public enum TruckLegRole
{
    /// <summary>Door to port: an export or domestic box going to the vessel.</summary>
    Pickup = 0,

    /// <summary>Port to door: an import or domestic box going to the consignee.</summary>
    Delivery = 1,

    /// <summary>Port or depot to yard and back, with no client door on either end.</summary>
    Transfer = 2
}
