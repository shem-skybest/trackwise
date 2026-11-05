using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

/// <summary>
/// DTO for broadcasting when a stop is marked as arrived.
/// </summary>
public record StopArrivalUpdateDto
{
    public Guid TripId { get; init; }
    public Guid StopId { get; init; }
    public int StopOrder { get; init; }
    public TripStopType StopType { get; init; }
    public Guid LoadId { get; init; }
    public string? LoadName { get; init; }
    public Address? Address { get; init; }
    public DateTime ArrivedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Reefer temperature (°C) at arrival, if captured.</summary>
    public decimal? TemperatureAtArrival { get; init; }

    /// <summary>
    /// Current progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage { get; init; }

    /// <summary>
    /// Whether this was the final stop (trip completed).
    /// </summary>
    public bool IsTripCompleted { get; init; }
}
