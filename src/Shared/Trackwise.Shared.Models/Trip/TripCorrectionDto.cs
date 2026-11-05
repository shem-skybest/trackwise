using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// One value changed on a trip after it was recorded. Values are text: a time in ISO 8601 UTC, a
/// number, or an enum member's name, depending on <see cref="Field" />.
/// </summary>
public record TripCorrectionDto
{
    public Guid Id { get; set; }

    /// <summary>The stop corrected; null for the garage times.</summary>
    public Guid? StopId { get; set; }

    public TripCorrectionField Field { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Reason { get; set; } = "";
    public string? CorrectedBy { get; set; }
    public DateTime CorrectedAt { get; set; }
}
