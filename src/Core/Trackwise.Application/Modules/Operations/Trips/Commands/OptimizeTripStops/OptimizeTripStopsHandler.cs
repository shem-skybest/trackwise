using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Routing;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class OptimizeTripStopsHandler :
    IAppRequestHandler<OptimizeTripStopsCommand, Result<OptimizedTripStopsDto>>
{
    private readonly ITripOptimizer _optimizer;
    private readonly IFerryRouteResolver _ferryResolver;
    private ILogger<OptimizeTripStopsHandler> _log;

    public OptimizeTripStopsHandler(
        ITripOptimizer optimizer, IFerryRouteResolver ferryResolver, ILogger<OptimizeTripStopsHandler> log)
    {
        _optimizer = optimizer;
        _ferryResolver = ferryResolver;
        _log = log;
    }

    public async Task<Result<OptimizedTripStopsDto>> Handle(OptimizeTripStopsCommand req, CancellationToken ct)
    {
        var stops = req.Stops.ToList();

        // Build request
        var optimizeTripParams = new OptimizeTripParams(
            stops,
            null,
            null, // optional
            req.MaxVehicles
        );

        // Optimize (Matrix or Heuristic fallback)
        var ordered = await _optimizer.OptimizeAsync(optimizeTripParams, ct);
        // Plain haversine is a straight line through open ocean for a PH inter-island pair - use
        // the real drive+ferry distance for those legs when one exists.
        var totalDistance = ordered.Zip(ordered.Skip(1), (a, b) =>
            _ferryResolver.TryResolve(a.Location, b.Location)?.TotalDistanceMeters
            ?? a.Location.DistanceTo(b.Location)).Sum();

        var result = new OptimizedTripStopsDto
        {
            TotalDistance = totalDistance,
            OrderedStops = ordered.ToList()
        };
        return Result<OptimizedTripStopsDto>.Ok(result);
    }
}
