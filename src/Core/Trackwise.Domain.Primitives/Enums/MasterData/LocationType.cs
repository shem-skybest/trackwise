namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// What kind of place a location record describes.
/// <para>
/// <see cref="Zone"/> is the odd one out and the reason the entity is self-referencing: a zone is
/// not somewhere a truck goes, it is the grouping the delivery points inside it are priced and
/// planned by. Modelling it as a parent rather than a second table keeps one list for the steward
/// and one set of machinery for the module.
/// </para>
/// </summary>
public enum LocationType
{
    /// <summary>A consignee address a delivery actually terminates at. The bulk of the list.</summary>
    DeliveryPoint = 0,

    /// <summary>A grouping of delivery points, used for pricing and planning rather than driven to.</summary>
    Zone = 1,

    Warehouse = 2,

    /// <summary>Container yard - empties and stacked stock.</summary>
    ContainerYard = 3,

    /// <summary>Off-dock depot, distinct from the port itself.</summary>
    Depot = 4,

    /// <summary>Customer plant or factory, as opposed to a distribution address.</summary>
    Plant = 5,

    /// <summary>Company-owned parking or motor pool.</summary>
    TruckYard = 6
}
