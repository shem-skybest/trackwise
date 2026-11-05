using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

/// <summary>
/// What a Cold Chain booking plan would cost, for the booking form to show before saving. A plan that
/// can't be priced comes back with <see cref="BookingEstimateDto.Error"/> set, not as a failure.
/// </summary>
public class EstimateColdChainBookingQuery : ColdChainBookingPlanRequest, IQuery<Result<BookingEstimateDto>>
{
    public Guid CustomerId { get; set; }

    /// <summary>The booking being edited, whose current sites may stay even if since deactivated.</summary>
    public Guid? BookingId { get; set; }
}
