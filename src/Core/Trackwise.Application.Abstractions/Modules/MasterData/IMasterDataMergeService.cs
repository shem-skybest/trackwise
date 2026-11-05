using Trackwise.Domain.Core;

namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// The entity-agnostic half of a merge: tombstoning the loser, repointing every cross-reference at
/// the survivor, and recording what happened.
/// <para>
/// Deciding which field wins is entity-specific and stays in the entity's handler, where it is
/// type-safe. Everything here is identical for every master entity and must not be reimplemented
/// per entity - getting the forwarding wrong is how old bookings start pointing at nothing.
/// </para>
/// </summary>
public interface IMasterDataMergeService
{
    /// <summary>
    /// Folds <paramref name="loser"/> into <paramref name="survivor"/>. The caller applies the
    /// steward's field choices to <paramref name="survivor"/> first; this method then stages the
    /// forwarding and repoints the dependent rows.
    /// <para>
    /// <b>Must be called inside <c>ITenantUnitOfWork.ExecuteInTransactionAsync</c></b>, and throws
    /// if it is not. Repointing dependent foreign keys is set-based SQL that takes effect
    /// immediately while the tombstone is only staged, so a merge that failed to save outside a
    /// transaction would leave the references already moved to a survivor nothing points at. The
    /// transaction belongs to the caller rather than here because the caller loads and edits the
    /// survivor, and a retry replays the unit against a cleared change tracker.
    /// </para>
    /// </summary>
    /// <returns>How many cross-reference rows were repointed.</returns>
    Task<int> MergeAsync(
        string entityType,
        IMasterDataRecord survivor,
        IMasterDataRecord loser,
        IReadOnlyDictionary<string, string>? fieldChoices,
        string? reason,
        CancellationToken ct = default);

    /// <summary>
    /// Follows the merge chain from an id that may belong to a record since merged away, and
    /// returns the id that is now authoritative.
    /// </summary>
    /// <returns>
    /// The surviving id, and whether forwarding actually happened so callers can tell the caller
    /// their reference was superseded rather than silently substituting a different record.
    /// </returns>
    Task<(Guid Id, bool Forwarded)> ResolveAsync(string entityType, Guid id, CancellationToken ct = default);
}
