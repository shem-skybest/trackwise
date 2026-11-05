using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Queries;

/// <summary>Every correction made to a trip's recorded values, newest first.</summary>
public sealed class GetTripCorrectionsQuery : IQuery<Result<List<TripCorrectionDto>>>
{
    public Guid TripId { get; set; }
}
