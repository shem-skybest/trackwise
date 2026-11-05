using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Books one Cold Chain truck-trip for a client, priced from the rate agreement in force on the
/// service date. Starts Pending, like Monday's "NEW BOOKING".
/// </summary>
public class CreateColdChainBookingCommand : ColdChainBookingPlanRequest, ICommand<Result<Guid>>
{
    public Guid CustomerId { get; set; }

    /// <summary>
    /// The number nSFS gave the booking, while numbers are typed in (see
    /// <see cref="SetBookingNumbersTypedCommand" />); may be left for later. Must be empty otherwise.
    /// </summary>
    public string? ReferenceNumber { get; set; }
}
