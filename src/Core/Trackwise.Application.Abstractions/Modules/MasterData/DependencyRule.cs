using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// One place the dependency probe looks for records that would be left pointing at a master record
/// a steward is about to retire.
/// </summary>
/// <param name="Label">
/// What the steward is told is still using the record, e.g. "trucks" or "open loads". Read verbatim
/// into the message, so it should be a plural noun phrase rather than a table name.
/// </param>
/// <param name="TableName">
/// Physical table holding the dependents. A constant from the policy table, never user input - the
/// probe interpolates it into SQL for the same reason the duplicate probe does.
/// </param>
/// <param name="Column">The foreign-key column in <paramref name="TableName"/> pointing at the record.</param>
/// <param name="Blocks">
/// Whether dependents here stop the write until the steward confirms. False makes the group
/// advisory - counted and reported, but not an obstacle. Use it for references that stay valid
/// after retirement, such as historical documents.
/// </param>
/// <param name="LiveCondition">
/// Optional extra SQL predicate narrowing the count to rows that actually matter, e.g.
/// <c>"deleted_at IS NULL"</c>. Trusted and compile-time like <paramref name="TableName"/>.
/// <para>
/// Usually worth setting: counting soft-deleted dependents makes the probe refuse a retirement
/// nothing real depends on any more, and a probe that cries wolf gets overridden by reflex.
/// </para>
/// </param>
public sealed record DependencyRule(
    string Label,
    string TableName,
    string Column,
    bool Blocks = true,
    string? LiveCondition = null)
{
    /// <summary>Which retiring modes this rule objects to. Delete is stricter than deactivate.</summary>
    public IReadOnlyList<MasterDataDeactivation> AppliesTo { get; init; } =
        [MasterDataDeactivation.Deactivate, MasterDataDeactivation.Delete];
}
