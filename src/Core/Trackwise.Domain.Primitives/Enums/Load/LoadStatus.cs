namespace Trackwise.Domain.Primitives.Enums;

public enum LoadStatus
{
    Draft, // created/quoted/booked but not dispatched yet
    Dispatched,
    PickedUp,
    Delivered,
    Cancelled, // aborted at any stage before Delivered

    /// <summary>
    /// Delivery attempted but rejected at the drop (wrong goods, site refused, etc.) - PH trucking
    /// term "Foul Trip". Distinct from <see cref="Cancelled"/> so it stays queryable as its own
    /// outcome rather than a free-text reason on a generic cancellation.
    /// </summary>
    FoulTrip,

    /// <summary>
    /// The client cancelled this specific drop after the load was already picked up - distinct
    /// from <see cref="Cancelled"/> for the same reporting reason as <see cref="FoulTrip"/>.
    /// </summary>
    CancelledByClient
}
