using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Turns a confirmed Cold Chain booking into a trip: one load per planned drop, from the pickup
/// warehouse, on the service date, then prices the trip from the client's agreement. The booking
/// moves to InExecution.
/// </summary>
public class CreateTripFromBookingCommand : ICommand<Result<CreateTripFromBookingResultDto>>
{
    public Guid BookingId { get; set; }
    public Guid? TruckId { get; set; }

    /// <summary>Leave the crew empty to take the crew planned for the truck that day.</summary>
    public Guid? DriverId { get; set; }

    public Guid? HelperId { get; set; }
    public Guid? SecondHelperId { get; set; }

    /// <summary>The signed-in dispatcher, set by the API.</summary>
    public Guid DispatcherId { get; set; }
}
