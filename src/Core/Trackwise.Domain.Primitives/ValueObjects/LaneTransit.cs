using System.Text.Json.Serialization;

namespace Trackwise.Domain.Primitives.ValueObjects;

public record LaneTransit
{
    public virtual IReadOnlyList<RouteStop> Stops { get; init; } = new List<RouteStop>();
    public virtual decimal TotalDistanceKm { get; init; }

    [JsonIgnore]
    public virtual RouteStop FirstPickup => Stops.First(s => s.Type == StopType.Pickup);

    [JsonIgnore]
    public virtual RouteStop FinalDrop => Stops.Last(s => s.Type == StopType.Dropoff);

    [JsonIgnore]
    public virtual int TotalStops => Stops.Count;

    [JsonIgnore]
    public virtual int IntermediateDropCount => Stops.Count(s => s.Type == StopType.Dropoff);

    // Public parameterless constructor required by EF Core Proxies & System.Text.Json
    public LaneTransit() { }

    public LaneTransit(IEnumerable<RouteStop> stops, decimal totalDistanceKm)
    {
        var orderedStops = stops.OrderBy(s => s.Sequence).ToList();

        if (orderedStops.Count < 2)
            throw new InvalidOperationException("A transit lane requires at least 2 stops (Origin and Destination).");

        if (!orderedStops.Any(s => s.Type == StopType.Pickup))
            throw new InvalidOperationException("Route must contain at least one Pickup stop.");

        if (!orderedStops.Any(s => s.Type == StopType.Dropoff))
            throw new InvalidOperationException("Route must contain at least one Dropoff stop.");

        if (totalDistanceKm <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalDistanceKm), "Total distance must be positive.");

        Stops = orderedStops;
        TotalDistanceKm = totalDistanceKm;
    }

    public static LaneTransit PointToPoint(
        Guid originLocationId,
        Guid destinationLocationId,
        DateTimeOffset pickupStart,
        DateTimeOffset deliveryEnd,
        decimal distanceKm)
    {
        var stops = new List<RouteStop>
        {
            RouteStop.Create(1, originLocationId, StopType.Pickup, pickupStart, null),
            RouteStop.Create(2, destinationLocationId, StopType.Dropoff, null, deliveryEnd)
        };

        return new LaneTransit(stops, distanceKm);
    }
}
