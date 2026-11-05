using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Switches where Cold Chain booking numbers come from: typed in from nSFS while it still issues
/// them, or issued by TrackWise once nSFS stops taking Cold Chain bookings.
/// </summary>
public class SetBookingNumbersTypedCommand : ICommand<Result<BookingNumberingDto>>
{
    public bool Typed { get; set; }
}
