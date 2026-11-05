using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Builds the right <see cref="Workfile"/> subclass for a shipment type, so callers pick a type
/// once instead of switching on it at every construction site.
/// </summary>
public static class WorkfileFactory
{
    public static Workfile Create(
        WorkfileType type,
        FreightMode? direction,
        TruckingVariant? variant,
        string referenceNumber,
        Guid customerId,
        Guid? bookingId,
        WorkfileHeaderData header) => type switch
    {
        WorkfileType.Reefer =>
            ReeferWorkfile.Create(referenceNumber, customerId, bookingId, header),
        WorkfileType.Trucking =>
            TruckingWorkfile.Create(
                referenceNumber, variant ?? TruckingVariant.Truck, customerId, bookingId, header),
        _ => RegularWorkfile.Create(
            referenceNumber, direction ?? FreightMode.Domestic, customerId, bookingId, header)
    };
}
