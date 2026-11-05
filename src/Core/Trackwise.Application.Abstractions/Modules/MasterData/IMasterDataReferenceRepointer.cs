namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// Moves the rows that physically point at a merged-away record over to the survivor.
/// <para>
/// The cross-reference table handles references held by <i>other</i> systems; this handles the ones
/// held inside this one, as foreign keys. Branch never needed it because nothing carried a
/// <c>branch_id</c> - but a merged port leaves live loads and containers pointing at a tombstone the
/// soft-delete filter hides, which reads to the user as the data having vanished.
/// </para>
/// <para>
/// The columns to move are the entity's own <c>MasterDataPolicy.DependencyRules</c>, which already
/// answer "who points at me" for the retirement probe, plus its <c>MergeReferences</c> - history
/// that must follow the record but may not block retiring it. A rule's <c>LiveCondition</c> is
/// deliberately <b>not</b> applied here - a delivered load pointing at the loser needs repointing
/// just as much as an in-flight one, since the loser stops resolving either way.
/// </para>
/// </summary>
public interface IMasterDataReferenceRepointer
{
    /// <summary>
    /// Repoints every dependent column declared for <paramref name="entityType"/> from
    /// <paramref name="loserId"/> to <paramref name="survivorId"/>.
    /// </summary>
    /// <returns>How many rows moved, across every column.</returns>
    Task<int> RepointAsync(
        string entityType,
        Guid survivorId,
        Guid loserId,
        CancellationToken ct = default);
}
