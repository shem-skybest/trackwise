namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// The canonical lifecycle every master record moves through. One state machine for all entities -
/// an entity's policy decides which transitions are reachable, it never introduces a second code
/// path.
/// <para>
/// The state is <b>derived</b>, not stored: it is read off the record's existing flags so that
/// <c>IsActive</c>, <c>DeletedAt</c> and <c>MergedIntoId</c> remain the single source of truth and
/// cannot drift from a duplicate status column.
/// </para>
/// </summary>
public enum MasterDataState
{
    /// <summary>Being prepared. Not live, not visible to consumers.</summary>
    Draft = 0,

    /// <summary>Submitted and awaiting approval. Not live.</summary>
    Pending = 1,

    /// <summary>Live. The only state offered for new work.</summary>
    Active = 2,

    /// <summary>
    /// Still resolves for existing references but is not offered for new work. Deliberately
    /// distinct from <see cref="Deleted"/>: deactivating means "no longer offered", deleting means
    /// "this was a mistake". Both keep the row.
    /// </summary>
    Inactive = 3,

    /// <summary>Soft-deleted and hidden by the global query filter. The row survives.</summary>
    Deleted = 4,

    /// <summary>
    /// Superseded by another record. Terminal - a merged record is never edited, restored or merged
    /// again; the survivor is. Reads forward through the cross-reference table.
    /// </summary>
    Merged = 5
}
