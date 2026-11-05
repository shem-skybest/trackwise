using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Sets the number the next Cold Chain booking gets this year, so BKN-RFV carries on from the last
/// number nSFS issued. Run once at cut-over; it refuses to go back to a number already issued.
/// </summary>
public class SetNextBookingNumberCommand : ICommand<Result<BookingNumberingDto>>
{
    public long NextNumber { get; set; }
}
