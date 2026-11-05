namespace Trackwise.Domain.Core;

/// <summary>
///     Marks a master record that belongs to one branch. A null <see cref="BranchId" /> means the
///     record applies everywhere.
///     <para>
///     Exactly one branch, never a set and never a join table. The legacy systems scoped records by
///     duplicating them per office, which is where the drift this module exists to remove came
///     from; pointing a single record at a branch is the replacement for that. Null is the escape
///     hatch for a record several branches genuinely share, so "exactly one" never forces a steward
///     to duplicate after all.
///     </para>
///     <para>
///     Retiring a branch is what makes this interface load-bearing: every table that gains a
///     <c>branch_id</c> belongs in <c>MasterDataPolicies.Branch.DependencyRules</c>, or a steward
///     can close an office out from under the records assigned to it.
///     </para>
/// </summary>
public interface IBranchScoped
{
    /// <summary>The owning branch, or <c>null</c> when the record applies to every branch.</summary>
    Guid? BranchId { get; set; }
}
