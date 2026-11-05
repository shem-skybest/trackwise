using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Records one of the four operational outcomes a booking slot can land on without becoming a
/// real, executed job: <see cref="BookingStatus.Resched"/>, <see cref="BookingStatus.Hold"/>,
/// <see cref="BookingStatus.LostOpportunity"/>, <see cref="BookingStatus.NoBooking"/>. Kept as one
/// command rather than four, unlike Workfile's one-verb-per-transition style - Booking itself has
/// no status machine, and these four are reporting outcomes, not stages a job is handed between
/// people for.
/// </summary>
public class SetBookingOperationalStatusCommand : ICommand<Result>
{
    public Guid Id { get; set; }

    /// <summary>Must be one of Resched, Hold, LostOpportunity, NoBooking.</summary>
    public BookingStatus Status { get; set; }

    public string? Note { get; set; }

    /// <summary>Why a Cold Chain booking was lost. Required for LostOpportunity on one.</summary>
    public BookingLossReason? LossReason { get; set; }
}
