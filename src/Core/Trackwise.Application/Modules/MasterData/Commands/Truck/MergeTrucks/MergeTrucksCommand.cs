using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Folds one truck into another. The steward picks the survivor and resolves each conflicting field
/// explicitly - there is deliberately no "newest wins".
/// <para>
/// Unlike a branch merge, this one moves real references: the loads, trips, expenses, fuel cards and
/// their transactions, papers, documents, DVIRs, accidents, maintenance and IFTA mileage pointing at
/// the loser are repointed at the survivor, per the truck policy's <c>DependencyRules</c> and
/// <c>MergeReferences</c>. ELD mappings and load-board postings stay on the loser: each is unique
/// per provider and truck, so moving them could collide with the survivor's own.
/// </para>
/// </summary>
public class MergeTrucksCommand : ICommand<Result<TruckDto>>
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
