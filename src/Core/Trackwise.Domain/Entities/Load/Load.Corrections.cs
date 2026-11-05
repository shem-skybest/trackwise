using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

public partial class Load
{
    /// <summary>
    /// Re-dates the load after its pickup's arrival was corrected. Only <see cref="Trip" /> calls
    /// this, once it has checked the new time.
    /// </summary>
    internal void CorrectPickup(DateTime arrivedAt)
    {
        PickedUpAt = arrivedAt;
    }

    /// <summary>
    /// Puts the load back where it was before its stop's arrival, which the crew took back. Only
    /// <see cref="Trip" /> calls this.
    /// </summary>
    /// <param name="pickedUp">Whether the load's pickup is still recorded, for a drop.</param>
    internal void UndoArrival(TripStopType stopType, bool pickedUp)
    {
        if (stopType == TripStopType.PickUp)
        {
            Status = LoadStatus.Dispatched;
            PickedUpAt = null;
            return;
        }

        Status = pickedUp ? LoadStatus.PickedUp : LoadStatus.Dispatched;
        DeliveredAt = null;
    }

    /// <summary>
    /// Takes back a drop's recorded outcome: the load reads delivered again, as its arrival left it.
    /// Only <see cref="Trip" /> calls this.
    /// </summary>
    internal void UndoOutcome()
    {
        Status = LoadStatus.Delivered;
        CancelledAt = null;
        ReturnedBoxes = null;
        ReturnedBags = null;
        ReturnReason = null;
        OutcomeNote = null;
    }

    /// <summary>
    /// Re-dates the load, and re-resolves it, after its drop's recorded event was corrected. Mirrors
    /// what recording did: arrival stamps <see cref="DeliveredAt" />, and a drop that didn't deliver
    /// stamps <see cref="CancelledAt" /> at its departure. Only <see cref="Trip" /> calls this, once
    /// it has checked the outcome fits the stop.
    /// </summary>
    internal void CorrectDrop(DateTime arrivedAt, DateTime? departedAt, LoadStatus? outcome)
    {
        DeliveredAt = arrivedAt;
        if (outcome is not { } resolved)
        {
            return;
        }

        Status = resolved;
        CancelledAt = resolved is LoadStatus.FoulTrip or LoadStatus.CancelledByClient or LoadStatus.Cancelled
            ? departedAt
            : null;
    }
}
