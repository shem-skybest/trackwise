using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

public record RouteLegDto
{
    public RouteLegMode Mode { get; set; }
    public required GeoPoint From { get; set; }
    public required GeoPoint To { get; set; }
    public double DistanceMeters { get; set; }
    public double DurationSeconds { get; set; }
    public string? Label { get; set; }
}
