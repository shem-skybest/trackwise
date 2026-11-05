using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

/// <summary>
/// The dispatcher's evening check: one day's Cold Chain bookings that have no trip yet - confirmed
/// ones waiting for a truck, and pending ones still to be confirmed.
/// </summary>
public class GetUnassignedBookingsQuery : IQuery<Result<List<UnAssignedBookingDto>>>
{
    public DateOnly Date { get; set; }
}
