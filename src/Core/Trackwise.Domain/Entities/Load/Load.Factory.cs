using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Factory methods for Load entity.
/// </summary>
public partial class Load
{
    /// <summary>
    /// Creates a new Load with an invoice (unless <paramref name="withInvoice"/> is false) and
    /// optional trip stops. Raises NewLoadCreatedEvent and LoadAssignedToTruckEvent if truck is assigned.
    /// </summary>
    public static Load Create(
        string name,
        LoadType type,
        decimal deliveryCost,
        Address originAddress,
        GeoPoint originLocation,
        Address destinationAddress,
        GeoPoint destinationLocation,
        Customer customer,
        Truck? assignedTruck,
        Employee? assignedDispatcher,
        Trip? trip = null,
        string currency = "USD",
        bool withInvoice = true)
    {
        return LoadFactory.Create(
            name,
            type,
            deliveryCost,
            originAddress,
            originLocation,
            destinationAddress,
            destinationLocation,
            customer,
            assignedTruck,
            assignedDispatcher,
            trip,
            currency,
            withInvoice);
    }
}
