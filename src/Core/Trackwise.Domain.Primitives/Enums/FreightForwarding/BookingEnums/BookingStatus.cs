namespace Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

public enum BookingStatus
{
    Draft = 0,       // auto-created from accepted quotation
    Confirmed = 1,   // documents + credit cleared -> triggers Billing & Trips
    Resched = 2,
    InProgress = 3,  // at least one trip dispatched
    Completed = 4,   // all trips delivered
    Hold = 5, // Neither confirmed or cancelled, waiting for customer ops
    Cancelled = 6
}
