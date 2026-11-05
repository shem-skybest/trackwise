using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Routing.Queries;

/// <summary>
/// Ferry-aware route between two points, for map rendering and distance display. Bound directly
/// from flat query params (a <see cref="Trackwise.Domain.Primitives.ValueObjects.GeoPoint" />
/// itself doesn't bind cleanly from a query string).
/// </summary>
public sealed class GetRouteSegmentsQuery : IQuery<Result<FerryAwareRouteDto>>
{
    public double OriginLng { get; set; }
    public double OriginLat { get; set; }
    public double DestinationLng { get; set; }
    public double DestinationLat { get; set; }
}
