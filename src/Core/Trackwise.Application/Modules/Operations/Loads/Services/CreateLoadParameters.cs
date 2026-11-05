using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Modules.Operations.Loads.Services;

/// <summary>
///     Create load parameters.
/// </summary>
/// <param name="Name">Load name</param>
/// <param name="Type">Load type</param>
/// <param name="Origin">Load origin geo point and address</param>
/// <param name="Destination">Load destination geo point and address</param>
/// <param name="DeliveryCost">Delivery cost</param>
/// <param name="Distance">Distance in meters</param>
/// <param name="CustomerId">Customer ID</param>
/// <param name="TruckId">Optional Truck ID - load can be created without truck assignment</param>
/// <param name="DispatcherId">Dispatcher ID</param>
/// <param name="TripId">Optionally specify trip ID</param>
/// <param name="Source">How this load entered the system. Defaults to Manual.</param>
/// <param name="RequestedPickupDate">Customer-requested pickup window date.</param>
/// <param name="RequestedDeliveryDate">Customer-requested delivery window date.</param>
/// <param name="Notes">Free-form notes for dispatcher / driver.</param>
/// <param name="ContainerId">Optional intermodal container being moved by this load.</param>
/// <param name="OriginTerminalId">Optional origin terminal (port / rail / depot).</param>
/// <param name="DestinationTerminalId">Optional destination terminal.</param>
/// <param name="OriginSiteId">Optional saved pickup site; must be the customer's own or shared.</param>
/// <param name="DestinationSiteId">Optional saved drop site; same rule as the origin.</param>
/// <param name="BookingId">Optional freight-forwarding Booking this load executes - must be Confirmed or InExecution.</param>
public record CreateLoadParameters(
    string Name,
    LoadType Type,
    (Address address, GeoPoint location) Origin,
    (Address address, GeoPoint location) Destination,
    decimal DeliveryCost,
    double Distance,
    Guid CustomerId,
    Guid? TruckId,
    Guid DispatcherId,
    Guid? TripId = null,
    LoadSource Source = LoadSource.Manual,
    DateTime? RequestedPickupDate = null,
    DateTime? RequestedDeliveryDate = null,
    string? Notes = null,
    Guid? ContainerId = null,
    Guid? OriginTerminalId = null,
    Guid? DestinationTerminalId = null,
    Guid? OriginSiteId = null,
    Guid? DestinationSiteId = null,
    Guid? BookingId = null,
    bool IsHazmat = false,
    HazmatClass? HazmatClass = null,
    string? UnNumber = null,
    string? Commodity = null,
    int? PlannedBoxes = null,
    int? PlannedBags = null
);
