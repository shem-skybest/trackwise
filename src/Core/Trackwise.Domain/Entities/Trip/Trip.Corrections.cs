using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Correcting what was recorded: a stop's event or the garage times, changed after the fact with a
/// reason. Recording stays once-only (<see cref="MarkStopArrived" />, <see cref="MarkStopDeparted(Guid, StopDeparture, string?)" />);
/// a correction is its own action, checked by the same rules, and every changed value is returned
/// as a <see cref="TripCorrection" /> for the caller to store.
/// </summary>
public partial class Trip
{
    private const int MaxReasonLength = 500;

    /// <summary>
    /// When something pricing reads last changed after the trip was priced: a drop's outcome
    /// corrected between delivered and refused, a drop added or removed, or a truck of another
    /// tonnage. A price set before this no longer matches the trip - see <see cref="IsPriceOutdated" />.
    /// </summary>
    public DateTime? PriceOutdatedSince { get; private set; }

    /// <summary>The trip was priced, then its drops, truck size or a drop's outcome changed: price it again.</summary>
    public bool IsPriceOutdated => PriceOutdatedSince is { } since && RatedAt is { } rated && since > rated;

    /// <summary>
    /// Corrects a stop's recorded arrival and, where recorded, its departure and outcome. A pickup's
    /// correction applies to its whole warehouse visit, as recording does.
    /// </summary>
    /// <param name="correction">The stop's whole recorded state as it should read.</param>
    /// <param name="reason">Why - required, and kept with every changed value.</param>
    /// <returns>One row per changed value; never empty.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the trip isn't under way or done, nothing is recorded on the stop, a recorded value
    /// would be cleared, a departure would be recorded for the first time, the times are in the future
    /// or out of order, the outcome doesn't fit the stop, or nothing changed.
    /// </exception>
    public IReadOnlyList<TripCorrection> CorrectStop(
        Guid stopId, StopCorrection correction, string reason, string? correctedBy)
    {
        EnsureCorrectable();
        reason = RequireReason(reason);

        var stop = FindStop(stopId);
        if (!stop.ArrivedAt.HasValue)
        {
            throw new InvalidOperationException("Nothing is recorded on this stop yet - enter it on the crew report.");
        }

        if (!stop.DepartedAt.HasValue && HasDeparture(correction))
        {
            throw new InvalidOperationException(
                "This stop's departure isn't recorded yet - enter it on the crew report, not as a correction.");
        }

        if (stop.DepartedAt.HasValue && !correction.DepartedAt.HasValue)
        {
            throw new InvalidOperationException("A recorded departure can't be cleared, only corrected.");
        }

        var visit = GetSameVisitPickups(stopId).Where(s => s.ArrivedAt.HasValue).ToList();
        var now = DateTime.UtcNow;
        foreach (var s in visit)
        {
            EnsureValidCorrection(s, correction, now);
            EnsureAfterRescue(s, correction.ArrivedAt);
            EnsureNoOverlap(s, correction.ArrivedAt, CorrectsDeparture(s, correction) ? correction.DepartedAt : s.DepartedAt);
        }

        EnsureGarageBrackets(visit, correction);

        var changes = visit.SelectMany(s => ApplyCorrection(s, correction, reason, correctedBy, now)).ToList();
        if (changes.Count == 0)
        {
            throw new InvalidOperationException("Nothing to correct - every value is the same as recorded.");
        }

        if (Status == TripStatus.Completed)
        {
            CompletedAt = Stops.Where(s => s.Type == TripStopType.DropOff).Max(s => s.DepartedAt);
        }

        return changes;
    }

    /// <summary>Corrects the recorded garage departure and/or arrival.</summary>
    /// <param name="departedAt">The garage departure as it should read; null only if none is recorded.</param>
    /// <param name="arrivedAt">The garage arrival as it should read; null only if none is recorded.</param>
    /// <returns>One row per changed value; never empty.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the trip isn't under way or done, a recorded time would be cleared or an unrecorded
    /// one set, a time is in the future or out of order with the stops, or nothing changed.
    /// </exception>
    public IReadOnlyList<TripCorrection> CorrectGarageTimes(
        DateTime? departedAt, DateTime? arrivedAt, string reason, string? correctedBy)
    {
        EnsureCorrectable();
        reason = RequireReason(reason);

        EnsureCorrectsRecorded("Garage departure", GarageDepartedAt, departedAt);
        EnsureCorrectsRecorded("Garage arrival", GarageArrivedAt, arrivedAt);

        var now = DateTime.UtcNow;
        if (departedAt is { } d)
        {
            EnsureNotInFuture(d, now, "Garage departure");
        }

        if (arrivedAt is { } a)
        {
            EnsureNotInFuture(a, now, "Garage arrival");
        }

        EnsureInOrder(
            ("Garage departure", departedAt),
            ("First arrival", OwnStops.Where(s => s.ArrivedAt.HasValue).Min(s => s.ArrivedAt)),
            ("Last departure", OwnStops.Where(s => s.DepartedAt.HasValue).Max(s => s.DepartedAt)),
            ("Garage arrival", arrivedAt));

        var changes = new List<TripCorrection>();
        if (!SameMinute(GarageDepartedAt, departedAt))
        {
            changes.Add(TripCorrection.Create(Id, null, TripCorrectionField.GarageDepartedAt,
                TripCorrection.Format(GarageDepartedAt), TripCorrection.Format(departedAt), reason, correctedBy, now));
            GarageDepartedAt = departedAt;
        }

        if (!SameMinute(GarageArrivedAt, arrivedAt))
        {
            changes.Add(TripCorrection.Create(Id, null, TripCorrectionField.GarageArrivedAt,
                TripCorrection.Format(GarageArrivedAt), TripCorrection.Format(arrivedAt), reason, correctedBy, now));
            GarageArrivedAt = arrivedAt;
        }

        if (changes.Count == 0)
        {
            throw new InvalidOperationException("Nothing to correct - the garage times are the same as recorded.");
        }

        return changes;
    }

    private void EnsureCorrectable()
    {
        if (Status is TripStatus.Draft or TripStatus.Cancelled)
        {
            throw new InvalidOperationException("Only a dispatched, in-transit or completed trip can be corrected.");
        }
    }

    private static string RequireReason(string? reason)
    {
        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new InvalidOperationException("Say why the recorded value is being corrected.");
        }

        if (trimmed.Length > MaxReasonLength)
        {
            throw new InvalidOperationException($"The reason can be at most {MaxReasonLength} characters.");
        }

        return trimmed;
    }

    private static bool HasDeparture(StopCorrection c) =>
        c.DepartedAt.HasValue || c.TemperatureAtDeparture.HasValue || c.HandlingStartedAt.HasValue
        || c.HandlingFinishedAt.HasValue || c.Outcome.HasValue || c.ReturnedBoxes.HasValue
        || c.ReturnedBags.HasValue || c.ReturnReason.HasValue || !string.IsNullOrWhiteSpace(c.OutcomeNote);

    private static void EnsureCorrectsRecorded(string what, DateTime? recorded, DateTime? corrected)
    {
        if (recorded.HasValue && !corrected.HasValue)
        {
            throw new InvalidOperationException($"{what} is recorded and can't be cleared, only corrected.");
        }

        if (!recorded.HasValue && corrected.HasValue)
        {
            throw new InvalidOperationException($"{what} isn't recorded yet - enter it on the crew report.");
        }
    }

    /// <summary>The same rules recording applies, against the stop's corrected values.</summary>
    private static void EnsureValidCorrection(TripStop stop, StopCorrection c, DateTime now)
    {
        EnsureNotInFuture(c.ArrivedAt, now, "Arrival");
        if (!CorrectsDeparture(stop, c))
        {
            return;
        }

        EnsureNotInFuture(c.DepartedAt!.Value, now, "Departure");
        EnsureInOrder(
            ("Arrival", c.ArrivedAt),
            ("Handling start", c.HandlingStartedAt),
            ("Handling finish", c.HandlingFinishedAt),
            ("Departure", c.DepartedAt));
        EnsureOutcomeFits(stop, ToDeparture(c));
    }

    /// <summary>
    /// Every stop time the trip's own truck ran, corrected ones included, must sit between the garage
    /// times. Rescued drops are the rescue truck's and aren't bracketed.
    /// </summary>
    private void EnsureGarageBrackets(IReadOnlyCollection<TripStop> corrected, StopCorrection c)
    {
        DateTime? ArrivalOf(TripStop s) => corrected.Contains(s) ? c.ArrivedAt : s.ArrivedAt;
        DateTime? DepartureOf(TripStop s) => corrected.Contains(s) && CorrectsDeparture(s, c) ? c.DepartedAt : s.DepartedAt;

        EnsureInOrder(
            ("Garage departure", GarageDepartedAt),
            ("First arrival", OwnStops.Select(ArrivalOf).Min()),
            ("Last departure", OwnStops.Select(DepartureOf).Max()),
            ("Garage arrival", GarageArrivedAt));
    }

    private List<TripCorrection> ApplyCorrection(
        TripStop stop, StopCorrection c, string reason, string? correctedBy, DateTime now)
    {
        var changes = new List<TripCorrection>();
        void Log(TripCorrectionField field, string? before, string? after) =>
            changes.Add(TripCorrection.Create(Id, stop.Id, field, before, after, reason, correctedBy, now));

        if (!SameMinute(stop.ArrivedAt, c.ArrivedAt))
        {
            Log(TripCorrectionField.ArrivedAt, TripCorrection.Format(stop.ArrivedAt), TripCorrection.Format(c.ArrivedAt));
            stop.ArrivedAt = c.ArrivedAt;
        }

        if (stop.TemperatureAtArrival != c.TemperatureAtArrival)
        {
            Log(TripCorrectionField.TemperatureAtArrival,
                TripCorrection.Format(stop.TemperatureAtArrival), TripCorrection.Format(c.TemperatureAtArrival));
            stop.TemperatureAtArrival = c.TemperatureAtArrival;
        }

        var load = stop.Load;
        if (stop.Type == TripStopType.PickUp)
        {
            load.CorrectPickup(stop.ArrivedAt!.Value);
        }

        if (!CorrectsDeparture(stop, c))
        {
            if (stop.Type == TripStopType.DropOff)
            {
                load.CorrectDrop(stop.ArrivedAt!.Value, null, null);
            }

            return changes;
        }

        if (!SameMinute(stop.DepartedAt, c.DepartedAt))
        {
            Log(TripCorrectionField.DepartedAt, TripCorrection.Format(stop.DepartedAt), TripCorrection.Format(c.DepartedAt));
            stop.DepartedAt = c.DepartedAt;
        }

        if (!SameMinute(stop.HandlingStartedAt, c.HandlingStartedAt))
        {
            Log(TripCorrectionField.HandlingStartedAt,
                TripCorrection.Format(stop.HandlingStartedAt), TripCorrection.Format(c.HandlingStartedAt));
            stop.HandlingStartedAt = c.HandlingStartedAt;
        }

        if (!SameMinute(stop.HandlingFinishedAt, c.HandlingFinishedAt))
        {
            Log(TripCorrectionField.HandlingFinishedAt,
                TripCorrection.Format(stop.HandlingFinishedAt), TripCorrection.Format(c.HandlingFinishedAt));
            stop.HandlingFinishedAt = c.HandlingFinishedAt;
        }

        if (stop.TemperatureAtDeparture != c.TemperatureAtDeparture)
        {
            Log(TripCorrectionField.TemperatureAtDeparture,
                TripCorrection.Format(stop.TemperatureAtDeparture), TripCorrection.Format(c.TemperatureAtDeparture));
            stop.TemperatureAtDeparture = c.TemperatureAtDeparture;
        }

        if (stop.Type == TripStopType.PickUp)
        {
            return changes;
        }

        var wasRefused = IsRefused(load.Status);
        if (load.Status != c.Outcome)
        {
            Log(TripCorrectionField.Outcome, load.Status.ToString(), TripCorrection.Format(c.Outcome));
        }

        load.CorrectDrop(stop.ArrivedAt!.Value, stop.DepartedAt, c.Outcome);
        if (wasRefused != IsRefused(load.Status))
        {
            PriceOutdatedSince = now;
        }

        if (load.ReturnedBoxes != c.ReturnedBoxes)
        {
            Log(TripCorrectionField.ReturnedBoxes, TripCorrection.Format(load.ReturnedBoxes), TripCorrection.Format(c.ReturnedBoxes));
            load.ReturnedBoxes = c.ReturnedBoxes;
        }

        if (load.ReturnedBags != c.ReturnedBags)
        {
            Log(TripCorrectionField.ReturnedBags, TripCorrection.Format(load.ReturnedBags), TripCorrection.Format(c.ReturnedBags));
            load.ReturnedBags = c.ReturnedBags;
        }

        if (load.ReturnReason != c.ReturnReason)
        {
            Log(TripCorrectionField.ReturnReason, TripCorrection.Format(load.ReturnReason), TripCorrection.Format(c.ReturnReason));
            load.ReturnReason = c.ReturnReason;
        }

        var note = string.IsNullOrWhiteSpace(c.OutcomeNote) ? null : c.OutcomeNote.Trim();
        if (load.OutcomeNote != note)
        {
            Log(TripCorrectionField.OutcomeNote, load.OutcomeNote, note);
            load.OutcomeNote = note;
        }

        return changes;
    }

    /// <summary>
    /// Only a recorded departure is corrected. Within a warehouse visit, a pickup whose departure is
    /// recorded is left alone when the corrected stop's isn't - its arrival is still corrected.
    /// </summary>
    private static bool CorrectsDeparture(TripStop stop, StopCorrection c) =>
        stop.DepartedAt.HasValue && c.DepartedAt.HasValue;

    private static StopDeparture ToDeparture(StopCorrection c) =>
        new(c.Outcome, c.TemperatureAtDeparture, c.DepartedAt, c.HandlingStartedAt, c.HandlingFinishedAt,
            c.ReturnedBoxes, c.ReturnedBags, c.ReturnReason, c.OutcomeNote);

    private static bool IsRefused(LoadStatus status) =>
        status is LoadStatus.FoulTrip or LoadStatus.CancelledByClient;

    /// <summary>
    /// The report page shows times to the minute, so a time sent back unchanged loses the seconds a
    /// "now" entry was stamped with. Within the same minute counts as unchanged.
    /// </summary>
    private static bool SameMinute(DateTime? recorded, DateTime? corrected) =>
        recorded is { } r && corrected is { } c
            ? ToMinute(r) == ToMinute(c)
            : recorded.HasValue == corrected.HasValue;

    private static long ToMinute(DateTime at) => at.ToUniversalTime().Ticks / TimeSpan.TicksPerMinute;
}
