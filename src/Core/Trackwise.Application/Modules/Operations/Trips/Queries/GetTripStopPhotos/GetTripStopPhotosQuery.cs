using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Queries;

/// <summary>Every photo attached to a trip's stops, oldest first.</summary>
public sealed class GetTripStopPhotosQuery : IQuery<Result<List<TripStopPhotoDto>>>
{
    public Guid TripId { get; set; }
}
