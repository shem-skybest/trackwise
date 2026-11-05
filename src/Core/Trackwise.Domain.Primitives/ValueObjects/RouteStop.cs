namespace Trackwise.Domain.Primitives.ValueObjects;

public enum StopType
{
    Pickup = 1,
    Dropoff = 2,
    CrossDockStaging = 3,
    Waypoint = 4
}

public record RouteStop
{
    public virtual int Sequence { get; init; }
    public virtual Guid LocationId { get; init; }
    public virtual StopType Type { get; init; }
    public virtual DateTimeOffset? WindowStart { get; init; }
    public virtual DateTimeOffset? WindowEnd { get; init; }
    public virtual string? Instructions { get; init; }

    public RouteStop() { }

    public static RouteStop Create(
        int sequence,
        Guid locationId,
        StopType type,
        DateTimeOffset? windowStart = null,
        DateTimeOffset? windowEnd = null,
        string? instructions = null)
    {
        if (sequence < 1)
            throw new ArgumentOutOfRangeException(nameof(sequence), "Stop sequence must start at 1.");

        if (windowStart.HasValue && windowEnd.HasValue && windowStart > windowEnd)
            throw new ArgumentException("Window start cannot be after window end.");

        return new RouteStop
        {
            Sequence = sequence,
            LocationId = locationId,
            Type = type,
            WindowStart = windowStart,
            WindowEnd = windowEnd,
            Instructions = instructions
        };
    }
}
