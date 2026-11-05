using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Folds one genset into another. The steward picks the survivor and resolves each conflicting
/// field explicitly - there is deliberately no "newest wins", because the duplicate often holds the
/// only correct serial number.
/// </summary>
public class MergeGensetsCommand : ICommand<Result<GensetDto>>
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
