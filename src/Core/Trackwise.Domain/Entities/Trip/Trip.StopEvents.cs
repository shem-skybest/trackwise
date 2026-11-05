using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Recording what happened on the road: arrival and departure at each stop, loading/unloading,
/// the drop's outcome, and the garage times. Every time is a reported time - office staff often
/// enter it hours later from a crew member's message - so each is checked against the others
/// rather than against the clock alone.
/// </summary>
public partial class Trip
{
    /// <summary>
    /// How far ahead of the server clock a reported time may be - a phone or PC clock running a
    /// little fast shouldn't turn a real "just now" into a rejected entry.
    /// </summary>
    private static readonly TimeSpan ClockSkewAllowance = TimeSpan.FromMinutes(5);

    /// <summary>Two pickups closer than this are one warehouse visit.</summary>
    private const double SameVisitRadiusMetres = 100;

    /// <summary>
    /// Marks a stop as arrived and updates the trip status accordingly. Arrival confirms
    /// pickup/delivery on the underlying load - a drop-off's real outcome (foul trip, cancelled by
    /// client, etc.) is recorded via <see cref="MarkStopDeparted(Guid, StopDeparture, string?)"/>, once known.
    /// </summary>
    /// <param name="stopId">The ID of the stop to mark as arrived.</param>
    /// <param name="temperatureAtArrival">Reefer temperature (°C) at arrival, if captured.</param>
    /// <param name="arrivedAt">When the truck actually arrived, in UTC. Defaults to now.</param>
    /// <param name="recordedBy">Who entered it.</param>
    /// <param name="source">Where it was entered.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the stop is not found, already arrived, or the time is in the future.
    /// </exception>
    public void MarkStopArrived(
        Guid stopId,
        decimal? temperatureAtArrival = null,
        DateTime? arrivedAt = null,
        string? recordedBy = null,
        TripEventSource source = TripEventSource.Office)
    {
        var stop = FindStop(stopId);
        if (stop.ArrivedAt.HasValue)
        {
            throw new InvalidOperationException("This stop is already marked as arrived.");
        }

        var now = DateTime.UtcNow;
        var at = arrivedAt ?? now;
        EnsureNotInFuture(at, now, "Arrival");
        EnsureCrewCanRecord(at, now, source);
        if (GarageDepartedAt is { } leftGarage && at < leftGarage)
        {
            throw new InvalidOperationException("Arrival can't be earlier than the garage departure.");
        }

        EnsureAfterRescue(stop, at);
        EnsureNoOverlap(stop, at, null);
        if (source == TripEventSource.DriverApp && OpenVisitElsewhere(stop) is { } open)
        {
            throw new InvalidOperationException($"Record the departure from {StopLabel(open)} first.");
        }

        stop.ArrivedAt = at;
        stop.ArrivalRecordedAt = now;
        stop.ArrivalRecordedBy = recordedBy;
        stop.ArrivalSource = source;
        stop.TemperatureAtArrival = temperatureAtArrival;

        var loadStatus = stop.Type == TripStopType.PickUp
            ? LoadStatus.PickedUp
            : LoadStatus.Delivered;

        stop.Load.UpdateStatus(loadStatus, force: true, at: at);

        RefreshStatus();
    }

    /// <summary>Records a departure with only an outcome, temperature and time - see the full overload.</summary>
    public void MarkStopDeparted(
        Guid stopId,
        LoadStatus? outcome,
        decimal? temperatureAtDeparture = null,
        DateTime? departedAt = null,
        string? recordedBy = null)
    {
        MarkStopDeparted(stopId, new StopDeparture(outcome, temperatureAtDeparture, departedAt), recordedBy);
    }

    /// <summary>
    /// Records departure from a stop. A drop-off needs its final outcome (delivered, foul trip,
    /// cancelled by client...) and may carry a partial return; a pickup has neither - leaving the
    /// warehouse doesn't change the load. Loading or unloading times already recorded on their own
    /// are kept; the departure's are used only where none is.
    /// </summary>
    /// <remarks>
    /// The outcome is applied with <c>force: true</c> deliberately: <see cref="MarkStopArrived"/>
    /// already force-set the load to <see cref="LoadStatus.Delivered"/> on arrival, a "terminal"
    /// status the ordinary state machine would refuse to leave. This call is the dispatcher
    /// finalizing that assumption with what actually happened.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the stop is not found, not yet arrived or already departed; a time is in the future
    /// or out of order (arrival ≤ handling start ≤ handling finish ≤ departure); the outcome doesn't
    /// fit the stop type; or the returned quantities are invalid.
    /// </exception>
    public void MarkStopDeparted(
        Guid stopId,
        StopDeparture departure,
        string? recordedBy = null,
        TripEventSource source = TripEventSource.Office)
    {
        var stop = FindStop(stopId);
        if (!stop.ArrivedAt.HasValue)
        {
            throw new InvalidOperationException("Mark the stop as arrived before recording its departure.");
        }

        if (stop.DepartedAt.HasValue)
        {
            throw new InvalidOperationException("This stop's departure is already recorded.");
        }

        EnsureOutcomeFits(stop, departure);

        var now = DateTime.UtcNow;
        var at = departure.DepartedAt ?? now;
        EnsureNotInFuture(at, now, "Departure");
        EnsureCrewCanRecord(at, now, source);
        EnsureInOrder(
            ("Arrival", stop.ArrivedAt),
            ("Handling start", stop.HandlingStartedAt ?? departure.HandlingStartedAt),
            ("Handling finish", stop.HandlingFinishedAt ?? departure.HandlingFinishedAt),
            ("Departure", at));
        EnsureNoOverlap(stop, stop.ArrivedAt.Value, at);

        stop.DepartedAt = at;
        stop.DepartureRecordedAt = now;
        stop.DepartureRecordedBy = recordedBy;
        stop.DepartureSource = source;
        stop.TemperatureAtDeparture = departure.Temperature;
        if (stop.HandlingStartedAt is null && departure.HandlingStartedAt is { } started)
        {
            SetHandlingStarted(stop, started, now, source);
        }

        if (stop.HandlingFinishedAt is null && departure.HandlingFinishedAt is { } finished)
        {
            SetHandlingFinished(stop, finished, now, source);
        }

        if (departure.Outcome is { } outcome)
        {
            stop.Load.UpdateStatus(outcome, force: true, at: at);
            stop.Load.ReturnedBoxes = departure.ReturnedBoxes;
            stop.Load.ReturnedBags = departure.ReturnedBags;
            stop.Load.ReturnReason = departure.ReturnReason;
            stop.Load.OutcomeNote = departure.OutcomeNote;
        }

        RefreshStatus();
    }

    /// <summary>
    /// The pickups that make up one warehouse visit with <paramref name="stopId"/>: every pickup on
    /// this trip within <see cref="SameVisitRadiusMetres"/> of it, itself included. Each load gets its
    /// own pickup stop, so a two-drop trip from one warehouse has two pickups for a single visit.
    /// </summary>
    public IReadOnlyList<TripStop> GetSameVisitPickups(Guid stopId)
    {
        var stop = FindStop(stopId);
        if (stop.Type != TripStopType.PickUp)
        {
            return [stop];
        }

        return Stops
            .Where(s => s.Type == TripStopType.PickUp
                        && s.Location.DistanceTo(stop.Location) <= SameVisitRadiusMetres)
            .OrderBy(s => s.Order)
            .ToList();
    }

    /// <summary>
    /// Records when the truck left the garage for this trip and when it got back - reported times,
    /// either of which may be set on its own. Departure must come before the first stop's arrival,
    /// and arrival after the last stop's departure.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the trip is a draft or cancelled, or a time is in the future or out of order.
    /// </exception>
    public void RecordGarageTimes(
        DateTime? departedAt, DateTime? arrivedAt, TripEventSource source = TripEventSource.Office)
    {
        if (Status is TripStatus.Draft or TripStatus.Cancelled)
        {
            throw new InvalidOperationException("Garage times can only be recorded on a dispatched trip.");
        }

        var now = DateTime.UtcNow;
        var departed = departedAt ?? GarageDepartedAt;
        var arrived = arrivedAt ?? GarageArrivedAt;
        if (departed is { } d)
        {
            EnsureNotInFuture(d, now, "Garage departure");
        }

        if (arrived is { } a)
        {
            EnsureNotInFuture(a, now, "Garage arrival");
        }

        // A rescued drop is the rescue truck's: the trip's own truck may be back at the garage before it.
        var firstArrival = OwnStops.Where(s => s.ArrivedAt.HasValue).Min(s => s.ArrivedAt);
        var lastDeparture = OwnStops.Where(s => s.DepartedAt.HasValue).Max(s => s.DepartedAt);
        EnsureInOrder(
            ("Garage departure", departed),
            ("First arrival", firstArrival),
            ("Last departure", lastDeparture),
            ("Garage arrival", arrived));

        if (departed != GarageDepartedAt)
        {
            GarageDepartedAt = departed;
            GarageDepartureRecordedAt = now;
            GarageDepartureSource = source;
        }

        if (arrived != GarageArrivedAt)
        {
            GarageArrivedAt = arrived;
            GarageArrivalRecordedAt = now;
            GarageArrivalSource = source;
        }
    }

    private static void EnsureOutcomeFits(TripStop stop, StopDeparture departure)
    {
        if (stop.Type == TripStopType.PickUp)
        {
            if (departure.Outcome.HasValue || departure.ReturnedBoxes.HasValue || departure.ReturnedBags.HasValue)
            {
                throw new InvalidOperationException("A pickup has no outcome or returns - only drop-offs do.");
            }

            return;
        }

        if (departure.Outcome is not { } outcome || !LoadStatusMachine.IsResolved(outcome))
        {
            throw new InvalidOperationException(
                "A drop-off needs a final outcome, such as delivered, foul trip or cancelled by client.");
        }

        var returnsAnything = departure.ReturnedBoxes > 0 || departure.ReturnedBags > 0;
        if (departure.ReturnedBoxes < 0 || departure.ReturnedBags < 0)
        {
            throw new InvalidOperationException("Returned quantities can't be negative.");
        }

        if (returnsAnything && outcome != LoadStatus.Delivered)
        {
            throw new InvalidOperationException(
                "Returned boxes or bags describe a partial delivery - record a foul trip or cancellation without them.");
        }

        if (returnsAnything && departure.ReturnReason is null)
        {
            throw new InvalidOperationException("Say why the boxes or bags came back.");
        }

        if (departure.ReturnedBoxes > stop.Load.PlannedBoxes || departure.ReturnedBags > stop.Load.PlannedBags)
        {
            throw new InvalidOperationException("More came back than was planned for this drop.");
        }
    }

    /// <summary>
    /// The truck is at one place at a time, so a stop's visit (arrival to departure, or just the
    /// arrival while it hasn't left) can't overlap another stop's. Stops at the same place - one
    /// warehouse visit's pickups, a backload picked up at a drop - share their times, and a rescue
    /// truck's drops run alongside the trip's own, so neither is compared.
    /// </summary>
    private void EnsureNoOverlap(TripStop stop, DateTime arrivedAt, DateTime? departedAt)
    {
        var end = departedAt ?? arrivedAt;
        foreach (var other in OtherPlacesOnTheSameTruck(stop))
        {
            if (other.ArrivedAt is not { } otherArrived)
            {
                continue;
            }

            var otherEnd = other.DepartedAt ?? otherArrived;
            if (arrivedAt < otherEnd && otherArrived < end)
            {
                throw new InvalidOperationException(
                    $"That overlaps the times recorded at {StopLabel(other)} - the truck can't be at two stops at once.");
            }
        }
    }

    /// <summary>A stop elsewhere the truck has arrived at and not yet left.</summary>
    private TripStop? OpenVisitElsewhere(TripStop stop) =>
        OtherPlacesOnTheSameTruck(stop).FirstOrDefault(s => s.ArrivedAt.HasValue && !s.DepartedAt.HasValue);

    private IEnumerable<TripStop> OtherPlacesOnTheSameTruck(TripStop stop) =>
        Stops.Where(s => s.RescueId == stop.RescueId
                         && s.Location.DistanceTo(stop.Location) > SameVisitRadiusMetres);

    private string StopLabel(TripStop stop) =>
        stop.Type == TripStopType.PickUp ? "the warehouse" : $"drop {DropNumber(stop)}";

    /// <summary>Each present time must not be earlier than the present time before it.</summary>
    private static void EnsureInOrder(params (string Label, DateTime? At)[] times)
    {
        (string Label, DateTime At)? previous = null;
        foreach (var (label, at) in times)
        {
            if (at is not { } current)
            {
                continue;
            }

            if (previous is { } p && current < p.At)
            {
                throw new InvalidOperationException($"{label} can't be earlier than {p.Label.ToLowerInvariant()}.");
            }

            previous = (label, current);
        }
    }

    private TripStop FindStop(Guid stopId) =>
        Stops.FirstOrDefault(s => s.Id == stopId)
        ?? throw new InvalidOperationException("Stop not found");

    private static void EnsureNotInFuture(DateTime at, DateTime now, string what)
    {
        if (at > now + ClockSkewAllowance)
        {
            throw new InvalidOperationException($"{what} time can't be in the future.");
        }
    }
}
