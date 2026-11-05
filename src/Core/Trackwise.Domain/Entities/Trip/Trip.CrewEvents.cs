using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// What the crew records from the Driver App, alongside the office's crew report. Both write the
/// same fields, once each, by whoever gets there first. The crew may set a time back a little
/// (a tap they forgot) and undo their own last tap for a few minutes; anything older is the office's
/// to enter or correct.
/// </summary>
public partial class Trip
{
    /// <summary>How far back the crew may date an event. Older ones are the office's to enter.</summary>
    public static readonly TimeSpan CrewBackdateWindow = TimeSpan.FromHours(2);

    /// <summary>How long the crew can take back their own tap.</summary>
    public static readonly TimeSpan CrewUndoWindow = TimeSpan.FromMinutes(10);

    /// <summary>When <see cref="GarageDepartedAt" /> was entered into the system.</summary>
    public DateTime? GarageDepartureRecordedAt { get; private set; }

    /// <summary>Where <see cref="GarageDepartedAt" /> was entered.</summary>
    public TripEventSource? GarageDepartureSource { get; private set; }

    /// <summary>When <see cref="GarageArrivedAt" /> was entered into the system.</summary>
    public DateTime? GarageArrivalRecordedAt { get; private set; }

    /// <summary>Where <see cref="GarageArrivedAt" /> was entered.</summary>
    public TripEventSource? GarageArrivalSource { get; private set; }

    /// <summary>
    /// Records when loading (at a pickup) or unloading (at a drop) started, before the departure.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the stop hasn't arrived, has departed or already has a start; or the time is in the
    /// future, out of order, or older than the crew may record.
    /// </exception>
    public void MarkHandlingStarted(
        Guid stopId, DateTime? startedAt = null, TripEventSource source = TripEventSource.Office)
    {
        var stop = FindHandlingStop(stopId);
        if (stop.HandlingStartedAt.HasValue)
        {
            throw new InvalidOperationException("The start is already recorded.");
        }

        var now = DateTime.UtcNow;
        var at = startedAt ?? now;
        EnsureNotInFuture(at, now, "Handling start");
        EnsureCrewCanRecord(at, now, source);
        EnsureInOrder(("Arrival", stop.ArrivedAt), ("Handling start", at), ("Handling finish", stop.HandlingFinishedAt));

        SetHandlingStarted(stop, at, now, source);
    }

    /// <summary>Records when loading or unloading finished, before the departure.</summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the stop hasn't arrived, has departed or already has a finish; or the time is in the
    /// future, out of order, or older than the crew may record.
    /// </exception>
    public void MarkHandlingFinished(
        Guid stopId, DateTime? finishedAt = null, TripEventSource source = TripEventSource.Office)
    {
        var stop = FindHandlingStop(stopId);
        if (stop.HandlingFinishedAt.HasValue)
        {
            throw new InvalidOperationException("The finish is already recorded.");
        }

        var now = DateTime.UtcNow;
        var at = finishedAt ?? now;
        EnsureNotInFuture(at, now, "Handling finish");
        EnsureCrewCanRecord(at, now, source);
        EnsureInOrder(("Arrival", stop.ArrivedAt), ("Handling start", stop.HandlingStartedAt), ("Handling finish", at));

        SetHandlingFinished(stop, at, now, source);
    }

    /// <summary>Records the garage departure once; a recorded one is corrected, not recorded again.</summary>
    public void MarkGarageDeparted(DateTime? departedAt = null, TripEventSource source = TripEventSource.Office)
    {
        if (GarageDepartedAt.HasValue)
        {
            throw new InvalidOperationException("The garage departure is already recorded.");
        }

        var now = DateTime.UtcNow;
        var at = departedAt ?? now;
        EnsureCrewCanRecord(at, now, source);
        RecordGarageTimes(at, null, source);
    }

    /// <summary>Records the garage arrival once; a recorded one is corrected, not recorded again.</summary>
    public void MarkGarageArrived(DateTime? arrivedAt = null, TripEventSource source = TripEventSource.Office)
    {
        if (GarageArrivedAt.HasValue)
        {
            throw new InvalidOperationException("The garage arrival is already recorded.");
        }

        var now = DateTime.UtcNow;
        var at = arrivedAt ?? now;
        EnsureCrewCanRecord(at, now, source);
        RecordGarageTimes(null, at, source);
    }

    /// <summary>
    /// Takes back an event the crew recorded from the app: only within <see cref="CrewUndoWindow" />,
    /// only while nothing was recorded after it, and never on a finished trip. A pickup's event is
    /// taken back for its whole warehouse visit, as it was recorded.
    /// </summary>
    /// <param name="stopId">The stop, for a stop event; null for a garage event.</param>
    /// <exception cref="InvalidOperationException">Thrown if the event can't be taken back.</exception>
    public void UndoCrewEvent(TripEventKind kind, Guid? stopId)
    {
        if (Status is TripStatus.Completed or TripStatus.Cancelled)
        {
            throw new InvalidOperationException("The trip is finished - ask the office to correct it.");
        }

        var visit = stopId is { } id ? GetSameVisitPickups(id) : [];
        var targets = RecordedEvents()
            .Where(e => e.Kind == kind && (e.Stop is null ? stopId is null : visit.Contains(e.Stop)))
            .ToList();
        if (targets.Count == 0)
        {
            throw new InvalidOperationException("There's nothing recorded to undo.");
        }

        if (targets.Any(e => e.Source != TripEventSource.DriverApp))
        {
            throw new InvalidOperationException("This was entered by the office - ask them to correct it.");
        }

        var recordedAt = targets.Max(e => e.RecordedAt);
        if (recordedAt < DateTime.UtcNow - CrewUndoWindow)
        {
            throw new InvalidOperationException(
                $"Only the last {CrewUndoWindow.TotalMinutes:0} minutes can be undone - ask the office to correct it.");
        }

        if (RecordedEvents().Except(targets).Any(e => e.RecordedAt > recordedAt))
        {
            throw new InvalidOperationException("Something was recorded after it - ask the office to correct it.");
        }

        foreach (var target in targets)
        {
            Revert(target);
        }

        if (Status == TripStatus.InTransit && !Stops.Any(s => s.Type == TripStopType.PickUp && s.ArrivedAt.HasValue))
        {
            Status = TripStatus.Dispatched;
        }
    }

    private void Revert(RecordedEvent e)
    {
        var stop = e.Stop;
        switch (e.Kind)
        {
            case TripEventKind.GarageDeparted:
                GarageDepartedAt = null;
                GarageDepartureRecordedAt = null;
                GarageDepartureSource = null;
                break;
            case TripEventKind.GarageArrived:
                GarageArrivedAt = null;
                GarageArrivalRecordedAt = null;
                GarageArrivalSource = null;
                break;
            case TripEventKind.Arrived:
                stop!.ArrivedAt = null;
                stop.ArrivalRecordedAt = null;
                stop.ArrivalRecordedBy = null;
                stop.ArrivalSource = null;
                stop.TemperatureAtArrival = null;
                stop.Load.UndoArrival(stop.Type, PickupArrived(stop.Load));
                break;
            case TripEventKind.HandlingStarted:
                stop!.HandlingStartedAt = null;
                stop.HandlingStartedRecordedAt = null;
                stop.HandlingStartedSource = null;
                break;
            case TripEventKind.HandlingFinished:
                stop!.HandlingFinishedAt = null;
                stop.HandlingFinishedRecordedAt = null;
                stop.HandlingFinishedSource = null;
                break;
            case TripEventKind.Departed:
                stop!.DepartedAt = null;
                stop.DepartureRecordedAt = null;
                stop.DepartureRecordedBy = null;
                stop.DepartureSource = null;
                stop.TemperatureAtDeparture = null;
                if (stop.Type == TripStopType.DropOff)
                {
                    stop.Load.UndoOutcome();
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e), e.Kind, null);
        }
    }

    private bool PickupArrived(Load load) =>
        Stops.Any(s => s.LoadId == load.Id && s.Type == TripStopType.PickUp && s.ArrivedAt.HasValue);

    /// <summary>Every event recorded on the trip, with when and where it was entered.</summary>
    private IEnumerable<RecordedEvent> RecordedEvents()
    {
        if (GarageDepartedAt.HasValue)
        {
            yield return new(null, TripEventKind.GarageDeparted, GarageDepartureRecordedAt, GarageDepartureSource);
        }

        if (GarageArrivedAt.HasValue)
        {
            yield return new(null, TripEventKind.GarageArrived, GarageArrivalRecordedAt, GarageArrivalSource);
        }

        foreach (var s in Stops)
        {
            if (s.ArrivedAt.HasValue)
            {
                yield return new(s, TripEventKind.Arrived, s.ArrivalRecordedAt, s.ArrivalSource);
            }

            if (s.HandlingStartedAt.HasValue)
            {
                yield return new(s, TripEventKind.HandlingStarted, s.HandlingStartedRecordedAt, s.HandlingStartedSource);
            }

            if (s.HandlingFinishedAt.HasValue)
            {
                yield return new(s, TripEventKind.HandlingFinished, s.HandlingFinishedRecordedAt, s.HandlingFinishedSource);
            }

            if (s.DepartedAt.HasValue)
            {
                yield return new(s, TripEventKind.Departed, s.DepartureRecordedAt, s.DepartureSource);
            }
        }
    }

    private TripStop FindHandlingStop(Guid stopId)
    {
        var stop = FindStop(stopId);
        if (!stop.ArrivedAt.HasValue)
        {
            throw new InvalidOperationException("Record the arrival first.");
        }

        if (stop.DepartedAt.HasValue)
        {
            throw new InvalidOperationException("This stop's departure is already recorded.");
        }

        return stop;
    }

    private static void SetHandlingStarted(TripStop stop, DateTime at, DateTime now, TripEventSource source)
    {
        stop.HandlingStartedAt = at;
        stop.HandlingStartedRecordedAt = now;
        stop.HandlingStartedSource = source;
    }

    private static void SetHandlingFinished(TripStop stop, DateTime at, DateTime now, TripEventSource source)
    {
        stop.HandlingFinishedAt = at;
        stop.HandlingFinishedRecordedAt = now;
        stop.HandlingFinishedSource = source;
    }

    private static void EnsureCrewCanRecord(DateTime at, DateTime now, TripEventSource source)
    {
        if (source == TripEventSource.DriverApp && at < now - CrewBackdateWindow)
        {
            throw new InvalidOperationException(
                $"Times more than {CrewBackdateWindow.TotalHours:0} hours ago are entered by the office.");
        }
    }

    /// <param name="Stop">The stop, or null for a garage event.</param>
    /// <param name="RecordedAt">When it was entered; null on events entered before this was kept.</param>
    private sealed record RecordedEvent(
        TripStop? Stop, TripEventKind Kind, DateTime? RecordedAt, TripEventSource? Source);
}
