using Trackwise.Application.Abstractions.Routing;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Infrastructure.Routing.Ferry;

/// <summary>
/// Resolves PH inter-island routes over the small port graph in <see cref="PhilippineFerryNetwork" />:
/// classify origin/destination onto a known island by bounding box, and - only when they differ -
/// find the drive+ferry path between them via Dijkstra, preferring the fewest ferry hops (a direct
/// sourced crossing over a hub-routed one) and breaking ties by duration.
/// </summary>
internal sealed class FerryRouteResolver : IFerryRouteResolver
{
    // Placeholder for the "drive to/from a port" legs, which aren't real ferry crossings and so
    // have no sourced figure - refined to a real number by the caller once Mapbox Directions runs
    // for that leg (see direction-map.ts's buildSegments). 50 km/h is a reasonable PH provincial-
    // road average for a loaded truck.
    private const double AssumedDrivingMetersPerSecond = 50_000.0 / 3600.0;

    // Added to a ferry edge's weight when pathfinding (not to the duration reported in the leg
    // itself). A composed multi-hop path (e.g. Negros -> Cebu -> Mindanao) can look "faster" than a
    // direct long-haul crossing on paper, purely because the same-island connector between hops is
    // a rough haversine drive estimate with none of a real transfer's port/customs/connection-risk
    // time built in. Dwarfing every real crossing's duration with this penalty makes the resolver
    // prefer the fewest ferry hops - i.e. take a direct sourced crossing over a hub-routed one -
    // and only fall back to composing through a hub (see PhilippineFerryNetwork) when no direct
    // crossing exists for that island pair.
    private const double FerryHopPenaltySeconds = 1_000_000;

    private readonly record struct Edge(string ToPortId, RouteLegMode Mode, double DistanceMeters, double DurationSeconds);

    private readonly Dictionary<string, FerryPort> portsById;
    private readonly Dictionary<string, List<Edge>> adjacency;

    public FerryRouteResolver()
    {
        portsById = PhilippineFerryNetwork.Ports.ToDictionary(p => p.Id);
        adjacency = PhilippineFerryNetwork.Ports.ToDictionary(p => p.Id, _ => new List<Edge>());

        foreach (var crossing in PhilippineFerryNetwork.Crossings)
        {
            AddEdge(crossing.PortAId, crossing.PortBId, RouteLegMode.Ferry, crossing.DistanceMeters, crossing.DurationSeconds);
            AddEdge(crossing.PortBId, crossing.PortAId, RouteLegMode.Ferry, crossing.DistanceMeters, crossing.DurationSeconds);
        }

        // Implicit same-island "drive between ports" edges - needed for an island that sits in the
        // middle of a path (Cebu, between Negros and Mindanao) to actually connect its two ports.
        foreach (var island in PhilippineFerryNetwork.Ports.Select(p => p.Island).Distinct())
        {
            var islandPorts = PhilippineFerryNetwork.Ports.Where(p => p.Island == island).ToList();
            for (var i = 0; i < islandPorts.Count; i++)
            {
                for (var j = i + 1; j < islandPorts.Count; j++)
                {
                    var meters = islandPorts[i].Location.DistanceTo(islandPorts[j].Location);
                    var seconds = meters / AssumedDrivingMetersPerSecond;
                    AddEdge(islandPorts[i].Id, islandPorts[j].Id, RouteLegMode.Driving, meters, seconds);
                    AddEdge(islandPorts[j].Id, islandPorts[i].Id, RouteLegMode.Driving, meters, seconds);
                }
            }
        }
    }

    public FerryAwareRouteDto? TryResolve(GeoPoint origin, GeoPoint destination)
    {
        var originIsland = Classify(origin);
        var destinationIsland = Classify(destination);

        if (originIsland is null || destinationIsland is null || originIsland == destinationIsland)
        {
            return null;
        }

        var entryCandidates = PhilippineFerryNetwork.Ports.Where(p => p.Island == originIsland).ToList();
        var exitCandidates = PhilippineFerryNetwork.Ports.Where(p => p.Island == destinationIsland).ToList();

        FerryPort? bestEntry = null;
        FerryPort? bestExit = null;
        List<string>? bestPath = null;
        var bestTotalSeconds = double.PositiveInfinity;

        foreach (var entry in entryCandidates)
        {
            var (distances, previous) = ShortestPathsFrom(entry.Id);

            foreach (var exit in exitCandidates)
            {
                if (!distances.TryGetValue(exit.Id, out var graphSeconds) || double.IsPositiveInfinity(graphSeconds))
                {
                    continue;
                }

                var totalSeconds =
                    origin.DistanceTo(entry.Location) / AssumedDrivingMetersPerSecond +
                    graphSeconds +
                    exit.Location.DistanceTo(destination) / AssumedDrivingMetersPerSecond;

                if (totalSeconds < bestTotalSeconds)
                {
                    bestTotalSeconds = totalSeconds;
                    bestEntry = entry;
                    bestExit = exit;
                    bestPath = ReconstructPath(previous, entry.Id, exit.Id);
                }
            }
        }

        if (bestEntry is null || bestExit is null || bestPath is null)
        {
            return null;
        }

        var legs = BuildLegs(origin, destination, bestEntry, bestExit, bestPath);
        return new FerryAwareRouteDto
        {
            Legs = legs,
            TotalDistanceMeters = legs.Sum(l => l.DistanceMeters),
            TotalDurationSeconds = legs.Sum(l => l.DurationSeconds),
        };
    }

    private List<RouteLegDto> BuildLegs(
        GeoPoint origin, GeoPoint destination, FerryPort entry, FerryPort exit, List<string> portPath)
    {
        var raw = new List<RouteLegDto>
        {
            DrivingLeg(origin, entry.Location),
        };

        for (var i = 0; i < portPath.Count - 1; i++)
        {
            var from = portsById[portPath[i]];
            var to = portsById[portPath[i + 1]];
            var edge = adjacency[from.Id].First(e => e.ToPortId == to.Id);

            raw.Add(edge.Mode == RouteLegMode.Ferry
                ? new RouteLegDto
                {
                    Mode = RouteLegMode.Ferry,
                    From = from.Location,
                    To = to.Location,
                    DistanceMeters = edge.DistanceMeters,
                    DurationSeconds = edge.DurationSeconds,
                    Label = $"Ferry crossing: {from.Name} → {to.Name}",
                }
                : DrivingLeg(from.Location, to.Location));
        }

        raw.Add(DrivingLeg(exit.Location, destination));

        // Consecutive Driving legs (e.g. origin -> entry port -> a second same-island port before
        // the first ferry) are an artifact of routing through named ports, not a real waypoint the
        // truck needs to stop at - merge them so the caller drives it as one leg.
        var merged = new List<RouteLegDto>();
        foreach (var leg in raw)
        {
            if (merged.Count > 0 && merged[^1].Mode == RouteLegMode.Driving && leg.Mode == RouteLegMode.Driving)
            {
                var previous = merged[^1];
                merged[^1] = previous with
                {
                    To = leg.To,
                    DistanceMeters = previous.DistanceMeters + leg.DistanceMeters,
                    DurationSeconds = previous.DurationSeconds + leg.DurationSeconds,
                };
                continue;
            }

            merged.Add(leg);
        }

        return merged;
    }

    private static RouteLegDto DrivingLeg(GeoPoint from, GeoPoint to)
    {
        var meters = from.DistanceTo(to);
        return new RouteLegDto
        {
            Mode = RouteLegMode.Driving,
            From = from,
            To = to,
            DistanceMeters = meters,
            DurationSeconds = meters / AssumedDrivingMetersPerSecond,
        };
    }

    private void AddEdge(string fromPortId, string toPortId, RouteLegMode mode, double distanceMeters, double durationSeconds)
    {
        adjacency[fromPortId].Add(new Edge(toPortId, mode, distanceMeters, durationSeconds));
    }

    private (Dictionary<string, double> Distances, Dictionary<string, string?> Previous) ShortestPathsFrom(string sourcePortId)
    {
        var distances = portsById.Keys.ToDictionary(id => id, _ => double.PositiveInfinity);
        var previous = new Dictionary<string, string?>();
        var visited = new HashSet<string>();
        distances[sourcePortId] = 0;

        while (visited.Count < portsById.Count)
        {
            string? currentId = null;
            var currentDistance = double.PositiveInfinity;

            foreach (var (id, distance) in distances)
            {
                if (!visited.Contains(id) && distance < currentDistance)
                {
                    currentId = id;
                    currentDistance = distance;
                }
            }

            if (currentId is null)
            {
                break;
            }

            visited.Add(currentId);

            foreach (var edge in adjacency[currentId])
            {
                var weight = edge.DurationSeconds + (edge.Mode == RouteLegMode.Ferry ? FerryHopPenaltySeconds : 0);
                var candidate = currentDistance + weight;
                if (candidate < distances[edge.ToPortId])
                {
                    distances[edge.ToPortId] = candidate;
                    previous[edge.ToPortId] = currentId;
                }
            }
        }

        return (distances, previous);
    }

    private static List<string> ReconstructPath(Dictionary<string, string?> previous, string sourceId, string targetId)
    {
        var path = new List<string> { targetId };
        var cursor = targetId;

        while (cursor != sourceId)
        {
            var prior = previous.GetValueOrDefault(cursor) ?? throw new InvalidOperationException(
                $"No path found from '{sourceId}' to '{targetId}' in the PH ferry port graph.");
            path.Add(prior);
            cursor = prior;
        }

        path.Reverse();
        return path;
    }

    private static PhilippineIsland? Classify(GeoPoint point)
    {
        foreach (var (island, bounds) in PhilippineFerryNetwork.IslandBoundingBoxes)
        {
            if (bounds.Contains(point))
            {
                return island;
            }
        }

        return null;
    }
}
