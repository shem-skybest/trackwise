using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Rescues: another truck and crew taking over drops the trip's own truck can no longer deliver.
/// The drops stay on this trip, which keeps its truck and price - the client booked one truck-trip -
/// and each taken-over drop records which rescue delivered it.
/// </summary>
public partial class Trip
{
    public virtual List<TripRescue> Rescues { get; private set; } = [];

    /// <summary>
    /// Records a rescue. The caller adds the returned row to its repository: it reaches the tracker
    /// only through <see cref="Rescues" />, which EF would save as an update.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the trip isn't under way; the truck is the trip's own; a helper is given twice; a stop
    /// isn't an unvisited drop of this trip or is already rescued; the transfer time is in the future
    /// or before the truck left the warehouse; or there is no reason.
    /// </exception>
    public TripRescue RecordRescue(RescueSpec spec, string? recordedBy)
    {
        if (Status is not (TripStatus.Dispatched or TripStatus.InTransit))
        {
            throw new InvalidOperationException("Only a trip under way can be rescued.");
        }

        if (spec.Truck.Id == TruckId)
        {
            throw new InvalidOperationException("The rescue truck must be another truck than the trip's own.");
        }

        if (spec.Helper is not null && spec.Helper.Id == spec.SecondHelper?.Id)
        {
            throw new InvalidOperationException("The same helper can't be assigned twice.");
        }

        if (string.IsNullOrWhiteSpace(spec.Reason))
        {
            throw new InvalidOperationException("Say why the trip needed a rescue.");
        }

        var stops = TakenOverStops(spec.StopIds);
        var now = DateTime.UtcNow;
        EnsureNotInFuture(spec.TransferredAt, now, "Transfer");

        var leftWarehouse = Stops.Where(s => s.Type == TripStopType.PickUp).Max(s => s.DepartedAt);
        EnsureInOrder(
            ("Garage departure", GarageDepartedAt),
            ("Warehouse departure", leftWarehouse),
            ("Transfer", spec.TransferredAt));

        var rescue = TripRescue.Create(Id, spec, recordedBy, now);
        Rescues.Add(rescue);
        foreach (var stop in stops)
        {
            stop.RescueId = rescue.Id;
        }

        return rescue;
    }

    /// <summary>
    /// Takes a rescue back, e.g. one recorded on the wrong trip. Allowed until one of its drops has a
    /// recorded arrival. The caller deletes the returned row.
    /// </summary>
    public TripRescue UndoRescue(Guid rescueId)
    {
        var rescue = Rescues.FirstOrDefault(r => r.Id == rescueId)
            ?? throw new InvalidOperationException("That rescue isn't on this trip.");

        var stops = Stops.Where(s => s.RescueId == rescueId).ToList();
        if (stops.Any(s => s.ArrivedAt.HasValue))
        {
            throw new InvalidOperationException(
                "The rescue truck has already reached one of its drops, so the rescue is part of the record now.");
        }

        foreach (var stop in stops)
        {
            stop.RescueId = null;
        }

        Rescues.Remove(rescue);
        return rescue;
    }

    /// <summary>
    /// Records when the rescue truck left its garage and got back, either on its own. Departure comes
    /// before the transfer; arrival after the last departure from a drop it took over.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the rescue isn't on this trip, the trip is cancelled, or a time is in the future or out of order.
    /// </exception>
    public void RecordRescueGarageTimes(Guid rescueId, DateTime? departedAt, DateTime? arrivedAt)
    {
        var rescue = Rescues.FirstOrDefault(r => r.Id == rescueId)
            ?? throw new InvalidOperationException("That rescue isn't on this trip.");

        if (Status == TripStatus.Cancelled)
        {
            throw new InvalidOperationException("This trip is cancelled.");
        }

        var now = DateTime.UtcNow;
        if (departedAt is { } d)
        {
            EnsureNotInFuture(d, now, "Rescue garage departure");
        }

        if (arrivedAt is { } a)
        {
            EnsureNotInFuture(a, now, "Rescue garage arrival");
        }

        var rescued = Stops.Where(s => s.RescueId == rescueId).ToList();
        EnsureInOrder(
            ("Rescue garage departure", departedAt),
            ("Transfer", rescue.TransferredAt),
            ("First rescued arrival", rescued.Min(s => s.ArrivedAt)),
            ("Last rescued departure", rescued.Max(s => s.DepartedAt)),
            ("Rescue garage arrival", arrivedAt));

        rescue.SetGarageTimes(departedAt, arrivedAt);
    }

    /// <summary>The stops the trip's own truck ran - every stop no rescue took over.</summary>
    private IEnumerable<TripStop> OwnStops => Stops.Where(s => s.RescueId is null);

    private List<TripStop> TakenOverStops(IReadOnlyCollection<Guid> stopIds)
    {
        if (stopIds.Count == 0)
        {
            throw new InvalidOperationException("Pick the drops the rescue truck took over.");
        }

        var stops = stopIds.Distinct().Select(FindStop).ToList();
        foreach (var stop in stops)
        {
            if (stop.Type != TripStopType.DropOff)
            {
                throw new InvalidOperationException("A rescue takes over drops; the warehouse stop stays with the trip.");
            }

            if (stop.ArrivedAt.HasValue)
            {
                throw new InvalidOperationException(
                    $"Drop {DropNumber(stop)} already has a recorded arrival, so the trip's own truck delivered it.");
            }

            if (stop.RescueId is not null)
            {
                throw new InvalidOperationException($"Drop {DropNumber(stop)} is already on a rescue.");
            }

            var loaded = Stops.Any(s => s.LoadId == stop.LoadId && s.Type == TripStopType.PickUp && s.ArrivedAt.HasValue);
            if (!loaded)
            {
                throw new InvalidOperationException(
                    $"Drop {DropNumber(stop)} hasn't been loaded yet - a rescue takes over goods already on the truck.");
            }
        }

        return stops;
    }

    /// <summary>A rescued drop can't be reached before its goods were moved to the rescue truck.</summary>
    private void EnsureAfterRescue(TripStop stop, DateTime arrivedAt)
    {
        if (stop.RescueId is { } rescueId
            && Rescues.FirstOrDefault(r => r.Id == rescueId) is { } rescue
            && arrivedAt < rescue.TransferredAt)
        {
            throw new InvalidOperationException(
                "Arrival can't be earlier than the transfer to the rescue truck.");
        }
    }

    private int DropNumber(TripStop stop) =>
        Stops.Where(s => s.Type == TripStopType.DropOff).OrderBy(s => s.Order).ToList().IndexOf(stop) + 1;
}
