namespace Trackwise.Shared.Models;

public record PermissionOptionDto
{
    public required string Name { get; init; }
    public required string Label { get; init; }

    /// <summary>Every staff role holds it; the editor shows it ticked and locked.</summary>
    public bool IsBasic { get; init; }
}
