using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Folds one location into another. Unlike a vessel or a commodity merge, this one moves real rows:
/// the delivery points sitting inside a merged zone are repointed at the survivor, per the column
/// the entity's policy already declares.
/// </summary>
public class MergeLocationsCommand : ICommand<Result<LocationDto>>
{
    /// <summary>The record that stays and inherits every reference the loser held.</summary>
    public Guid SurvivorId { get; set; }

    /// <summary>The record folded in. Becomes a tombstone that forwards to the survivor.</summary>
    public Guid LoserId { get; set; }

    /// <summary>
    /// Field name -> <c>"survivor"</c> or <c>"loser"</c>. Anything omitted keeps the survivor's
    /// value. Recorded verbatim in the merge history so the decision can be reviewed later.
    /// </summary>
    public Dictionary<string, string> FieldChoices { get; set; } = [];

    public string? Reason { get; set; }
}
