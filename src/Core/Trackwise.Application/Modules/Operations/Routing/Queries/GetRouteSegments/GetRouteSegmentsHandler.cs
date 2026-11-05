using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Routing;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Routing.Queries;

internal sealed class GetRouteSegmentsHandler(IFerryRouteResolver ferryResolver) :
    IAppRequestHandler<GetRouteSegmentsQuery, Result<FerryAwareRouteDto>>
{
    // Only used for the plain single-leg fallback below - the client always re-fetches the real
    // distance/duration for a Driving leg from Mapbox Directions, so this is a rough placeholder,
    // not a persisted number.
    private const double AssumedDrivingMetersPerSecond = 50_000.0 / 3600.0;

    public Task<Result<FerryAwareRouteDto>> Handle(GetRouteSegmentsQuery req, CancellationToken ct)
    {
        var origin = new GeoPoint(req.OriginLng, req.OriginLat);
        var destination = new GeoPoint(req.DestinationLng, req.DestinationLat);

        var route = ferryResolver.TryResolve(origin, destination) ?? SingleDrivingLeg(origin, destination);
        return Task.FromResult(Result<FerryAwareRouteDto>.Ok(route));
    }

    private static FerryAwareRouteDto SingleDrivingLeg(GeoPoint origin, GeoPoint destination)
    {
        var meters = origin.DistanceTo(destination);
        var leg = new RouteLegDto
        {
            Mode = RouteLegMode.Driving,
            From = origin,
            To = destination,
            DistanceMeters = meters,
            DurationSeconds = meters / AssumedDrivingMetersPerSecond,
        };

        return new FerryAwareRouteDto
        {
            Legs = [leg],
            TotalDistanceMeters = leg.DistanceMeters,
            TotalDurationSeconds = leg.DurationSeconds,
        };
    }
}
