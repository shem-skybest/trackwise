using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.ConfirmBooking;

public class ConfirmBookingCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public string ConfirmedBy { get; set; } = default!;
    public string? CarrierReference { get; set; }
}
