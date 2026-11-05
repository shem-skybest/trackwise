using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Folds one port into another. The steward picks the survivor and resolves each conflicting field
/// explicitly - there is deliberately no "newest wins", because the duplicate often holds the
/// better address or the only correct country code.
/// <para>
/// Unlike a branch merge, this one moves real references: the loads and containers pointing at the
/// loser are repointed at the survivor, per the columns the entity's policy declares.
/// </para>
/// </summary>
public class MergePortsCommand : ICommand<Result<TerminalDto>>
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
