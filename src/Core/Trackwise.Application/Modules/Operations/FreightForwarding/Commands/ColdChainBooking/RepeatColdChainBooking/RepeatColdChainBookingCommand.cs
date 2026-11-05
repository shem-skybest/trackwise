using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// A standing order: copies a Cold Chain booking's plan onto each chosen day, each copy its own
/// booking with its own BKN-RFV number and estimate. All or nothing - one day the client's
/// agreement doesn't cover stops the lot.
/// </summary>
public class RepeatColdChainBookingCommand : ICommand<Result<List<RepeatedBookingDto>>>
{
    public Guid BookingId { get; set; }
    public List<DateOnly> Dates { get; set; } = [];

    /// <summary>Confirm the copies straight away, for a client whose standing order is firm.</summary>
    public bool Confirm { get; set; }

    /// <summary>The signed-in user, set by the API.</summary>
    public string ConfirmedBy { get; set; } = default!;
}
