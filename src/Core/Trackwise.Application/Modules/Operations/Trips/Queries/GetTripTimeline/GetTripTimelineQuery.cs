using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Queries;

public sealed class GetTripTimelineQuery : IQuery<Result<TripTimelineDto>>
{
    public Guid TripId { get; set; }
}
