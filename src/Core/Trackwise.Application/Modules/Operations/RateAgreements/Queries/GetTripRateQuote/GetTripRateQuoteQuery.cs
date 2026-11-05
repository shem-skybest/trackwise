using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Queries;

/// <summary>What the trip would cost under its client's agreement, without applying it.</summary>
public class GetTripRateQuoteQuery : IQuery<Result<TripRateQuoteDto>>
{
    public Guid TripId { get; set; }
    public DateOnly? RatedOn { get; set; }
}
