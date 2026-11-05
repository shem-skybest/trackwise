using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Domain event methods for Trip entity.
/// </summary>
public partial class Trip
{
    /// <summary>
    /// Assigns or reassigns this trip to a truck and raises the TripAssignedToTruckEvent.
    /// </summary>
    /// <param name="newTruck">The new truck to assign this trip to.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the trip is under way or done, or a stop's arrival is already recorded.
    /// </exception>
    public void AssignToTruck(Truck newTruck)
    {
        if (!TripStatusMachine.CanModify(Status))
        {
            throw new InvalidOperationException("Cannot change truck assignment unless trip is Draft.");
        }

        EnsureNoStopRecorded("The truck");
        if (BookedTruckSize is null && Truck?.TonnageClass != newTruck.TonnageClass)
        {
            MarkPriceOutdated();
        }

        var oldTruck = Truck;
        var oldTruckId = TruckId;
        var oldDriverDeviceToken = (Driver ?? oldTruck?.MainDriver)?.DeviceToken;

        TruckId = newTruck.Id;
        Truck = newTruck;

        TripFactory.RaiseTruckAssignedEvent(
            this,
            newTruck,
            oldTruckId,
            oldDriverDeviceToken);
    }
}
