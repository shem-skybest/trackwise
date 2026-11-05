using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Queries;

public sealed class GetTripQuery : IQuery<Result<TripDto>>
{
    public Guid TripId { get; set; }
}
