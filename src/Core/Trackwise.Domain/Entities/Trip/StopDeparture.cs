using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Everything recorded when a truck leaves a stop. Times are reported times in UTC; null means
/// "now" for <see cref="DepartedAt" /> and "not recorded" for the handling times.
/// </summary>
/// <param name="Outcome">
/// A drop's final outcome (a resolved load status such as delivered or foul trip); null for a
/// pickup.
/// </param>
/// <param name="ReturnedBoxes">Boxes that came back on the truck (a partial delivery); drops only.</param>
/// <param name="ReturnedBags">Bags that came back on the truck; drops only.</param>
/// <param name="ReturnReason">Why anything came back; required when anything did.</param>
/// <param name="OutcomeNote">What happened, in the staff's words.</param>
public sealed record StopDeparture(
    LoadStatus? Outcome,
    decimal? Temperature = null,
    DateTime? DepartedAt = null,
    DateTime? HandlingStartedAt = null,
    DateTime? HandlingFinishedAt = null,
    int? ReturnedBoxes = null,
    int? ReturnedBags = null,
    DropReturnReason? ReturnReason = null,
    string? OutcomeNote = null);
