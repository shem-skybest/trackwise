using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.Routing;

namespace Trackwise.Infrastructure.Routing.Optimization;

internal sealed class MapboxMatrixTripOptimizer(MapboxMatrixClient matrix, IFerryRouteResolver ferryResolver) : ITripOptimizer
{
    public async Task<IReadOnlyList<TripStopDto>> OptimizeAsync(OptimizeTripParams @params,
        CancellationToken ct = default)
    {
        // 1) Build coordinate list and index mapping
        var coords = new List<(double lng, double lat)>();
        var idxOf = new Dictionary<Guid, int>(); // stopId -> matrix index

        var startIdx = -1;

        if (@params.Start is not null)
        {
            startIdx = coords.Count;
            coords.Add((@params.Start.Longitude, @params.Start.Latitude));
        }

        foreach (var s in @params.Stops.OrderBy(s => s.Order))
        {
            idxOf[s.Id] = coords.Count;
            coords.Add((s.Location.Longitude, s.Location.Latitude));
        }

        if (@params.End is not null)
        {
            coords.Add((@params.End.Longitude, @params.End.Latitude));
        }

        // 2) Query Matrix
        var matrix1 = await matrix.GetMatrixAsync(coords, ct);

        // Mapbox's Matrix API is driving-only and has no ferry profile - for a PH inter-island
        // pair it still returns "Ok" with a fabricated road cost across open ocean. Overwrite
        // those cells with the real drive+ferry distance/duration before they can corrupt the
        // greedy stop-ordering below; same-island cells are left as Mapbox's real numbers.
        ApplyFerryOverrides(coords, matrix1);

        // 3) Greedy PD with capacity
        var remaining = @params.Stops.ToDictionary(s => s.Id, s => s);
        var pickedLoads = new HashSet<Guid>(); // LoadIds already picked
        var route = new List<TripStopDto>(@params.Stops.Count);

        // start from depot if exists else from first stop
        var curIdx = startIdx >= 0 ? startIdx : idxOf[@params.Stops[0].Id];
        var capacity = 0;

        while (remaining.Count > 0)
        {
            TripStopDto? best = null;
            var bestCost = double.PositiveInfinity;

            foreach (var s in remaining.Values)
            {
                // precedence
                if (s.Type == TripStopType.DropOff && !pickedLoads.Contains(s.LoadId))
                {
                    continue;
                }

                // capacity
                var nextCapacity = capacity + s.GetDemand();
                if (nextCapacity < 0 || nextCapacity > @params.VehicleCapacity)
                {
                    continue;
                }

                // cost from current to candidate
                var toIdx = idxOf[s.Id];
                var cost = matrix1.Cost(curIdx, toIdx);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = s;
                }
            }

            if (best is null)
            {
                // infeasible with matrix-driven choice -> bail to caller/fallback
                throw new InvalidOperationException("No feasible next stop found (capacity/precedence block).");
            }

            // commit choice
            route.Add(best);
            capacity += best.GetDemand();
            if (best.Type == TripStopType.PickUp)
            {
                pickedLoads.Add(best.LoadId);
            }

            curIdx = idxOf[best.Id];
            remaining.Remove(best.Id);
        }

        // 4) Renumber orders
        for (var i = 0; i < route.Count; i++)
        {
            route[i].Order = i + 1;
        }

        return route;
    }

    private void ApplyFerryOverrides(List<(double lng, double lat)> coords, MapboxMatrixResult matrix1)
    {
        for (var i = 0; i < coords.Count; i++)
        {
            for (var j = 0; j < coords.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var origin = new GeoPoint(coords[i].lng, coords[i].lat);
                var destination = new GeoPoint(coords[j].lng, coords[j].lat);
                var ferryRoute = ferryResolver.TryResolve(origin, destination);

                if (ferryRoute is null)
                {
                    continue;
                }

                matrix1.Distances[i][j] = ferryRoute.TotalDistanceMeters;
                matrix1.Durations[i][j] = ferryRoute.TotalDurationSeconds;
            }
        }
    }
}
