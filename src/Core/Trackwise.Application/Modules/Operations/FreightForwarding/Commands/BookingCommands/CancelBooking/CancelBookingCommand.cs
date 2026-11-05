using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.CancelBooking;

public class CancelBookingCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public string CancelledBy { get; set; } = default!;

    /// <summary>Free text. Optional when <see cref="LossReason"/> says why.</summary>
    public string? Reason { get; set; }

    public CancellationParty? CancelledByParty { get; set; }

    /// <summary>Required for a Cold Chain booking.</summary>
    public BookingLossReason? LossReason { get; set; }
}
