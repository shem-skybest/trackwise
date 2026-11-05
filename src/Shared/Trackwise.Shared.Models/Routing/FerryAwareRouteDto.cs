namespace Trackwise.Shared.Models;

public record FerryAwareRouteDto
{
    public required IReadOnlyList<RouteLegDto> Legs { get; set; }
    public double TotalDistanceMeters { get; set; }
    public double TotalDurationSeconds { get; set; }
}
