using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Folds one employee into another. The steward picks the survivor and resolves each conflicting
/// field explicitly - there is deliberately no "newest wins".
/// <para>
/// Because <c>Employee.Id</c> is the same id as a master-database <c>User</c>, merging away a loser
/// whose login is still live has the same side effect as deleting it - see the handler's live-login
/// guard, gated by <see cref="ConfirmedDespiteDependents" /> the same way any other dependency-in-use
/// case is.
/// </para>
/// </summary>
public class MergeEmployeesCommand : ICommand<Result<EmployeeDto>>
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

    /// <summary>Required when the losing employee's id still belongs to a live platform login.</summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }
}
