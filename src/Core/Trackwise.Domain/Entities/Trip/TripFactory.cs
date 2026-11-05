using Trackwise.Domain.Events;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Factory for creating Trip entities with all required related entities.
/// </summary>
public static class TripFactory
{
    /// <summary>
    /// Creates a new Trip with stops for the provided loads.
    /// Raises NewTripCreatedEvent and TripAssignedToTruckEvent if truck is assigned. The driver
    /// defaults to the truck's main driver when none is given.
    /// </summary>
    public static Trip Create(
        string name,
        Truck? truck,
        IEnumerable<Load>? loads = null,
        IEnumerable<TripStop>? optimizedStops = null,
        double? optimizedTotalDistance = null,
        Helper? helper = null,
        Employee? driver = null,
        Helper? secondHelper = null)
    {
        var loadsArr = loads?.ToArray() ?? [];

        var trip = new Trip
        {
            Name = name,
            TruckId = truck?.Id,
            Truck = truck,
            TotalDistance = optimizedTotalDistance ?? loadsArr.Sum(l => l.Distance)
        };

        trip.AssignCrew(driver ?? truck?.MainDriver, helper, secondHelper);

        if (optimizedStops is not null)
        {
            foreach (var stop in optimizedStops)
            {
                stop.Trip = trip;
                stop.TripId = trip.Id;
                trip.Stops.Add(stop);
            }
        }
        else
        {
            AddStopsForLoads(trip, loadsArr);
        }

        trip.DomainEvents.Add(new NewTripCreatedEvent(trip.Id));

        if (truck is not null)
        {
            RaiseTruckAssignedEvent(trip, truck, oldTruckId: null, oldDriverDeviceToken: null);
        }

        return trip;
    }

    /// <summary>
    /// Adds the loads' pickups, then their drop-offs in the same load order. A truck loads
    /// everything before it delivers, so a booking's drops from one warehouse read as one
    /// visit followed by the drops rather than pickup, drop, pickup, drop.
    /// </summary>
    public static void AddStopsForLoads(Trip trip, IEnumerable<Load> loads, int? startingOrder = null)
    {
        var order = startingOrder ?? (trip.Stops.Count == 0 ? 1 : trip.Stops.Max(s => s.Order) + 1);
        var loadsArr = loads.ToArray();

        foreach (var load in loadsArr)
        {
            trip.Stops.Add(new TripStop
            {
                Trip = trip,
                TripId = trip.Id,
                Order = order++,
                Type = TripStopType.PickUp,
                Address = load.OriginAddress,
                Location = load.OriginLocation,
                Load = load,
                LoadId = load.Id
            });
        }

        foreach (var load in loadsArr)
        {
            trip.Stops.Add(new TripStop
            {
                Trip = trip,
                TripId = trip.Id,
                Order = order++,
                Type = TripStopType.DropOff,
                Address = load.DestinationAddress,
                Location = load.DestinationLocation,
                Load = load,
                LoadId = load.Id
            });
        }
    }

    internal static void RaiseTruckAssignedEvent(
        Trip trip,
        Truck newTruck,
        Guid? oldTruckId,
        string? oldDriverDeviceToken)
    {
        var driver = trip.Driver ?? newTruck.MainDriver;
        var driverDisplayName = driver?.GetFullName() ?? newTruck.Number;

        trip.DomainEvents.Add(new TripAssignedToTruckEvent(
            trip.Id,
            trip.Number,
            trip.Name,
            newTruck.Id,
            newTruck.Number,
            driver?.DeviceToken,
            driverDisplayName,
            oldTruckId,
            oldDriverDeviceToken));
    }
}
