namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// What a write is about to do to a master record's availability, which decides whether the
/// dependency probe runs and how hard it pushes back.
/// <para>
/// The two retiring modes are deliberately distinct, mirroring
/// <see cref="MasterDataState.Inactive"/> versus <see cref="MasterDataState.Deleted"/>:
/// deactivating means "no longer offered", deleting means "this was a mistake". A record with
/// dependents can legitimately be deactivated - an office closes and its old loads keep pointing at
/// it - but it can rarely have been a mistake.
/// </para>
/// </summary>
public enum MasterDataDeactivation
{
    /// <summary>An ordinary edit that leaves the record available. Nothing to probe.</summary>
    None = 0,

    /// <summary>Clearing <c>IsActive</c>. The record still resolves; it stops being offered.</summary>
    Deactivate = 1,

    /// <summary>Soft delete. The row survives but leaves ordinary views entirely.</summary>
    Delete = 2
}
