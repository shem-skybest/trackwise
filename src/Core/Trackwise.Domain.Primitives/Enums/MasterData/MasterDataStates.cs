namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Reads a master record's <see cref="MasterDataState"/> off the flags it already carries.
/// <para>
/// Shared rather than written per caller because the precedence is the load-bearing part and it is
/// easy to get subtly wrong: a merged record is also soft-deleted, and a pending record is also
/// inactive, so an order that tested the wrong flag first would report both as something they are
/// not. <c>ChangeLogSaveChangesInterceptor</c> classifies lifecycle changes in the same order for
/// the same reason.
/// </para>
/// </summary>
public static class MasterDataStates
{
    /// <summary>
    /// The state of a record with the given flags. <paramref name="approval"/> defaults to
    /// <see cref="MasterDataApprovalState.Approved"/>, which is what an entity with no
    /// maker-checker policy is: nothing was ever waiting on a second person.
    /// </summary>
    public static MasterDataState Derive(
        bool isActive,
        DateTime? deletedAt,
        Guid? mergedIntoId,
        MasterDataApprovalState approval = MasterDataApprovalState.Approved) =>
        mergedIntoId is not null ? MasterDataState.Merged
        : deletedAt is not null ? MasterDataState.Deleted

        // Ahead of IsActive, not after it. A draft and a pending record are both inactive - that is
        // what stops them being offered for work - so testing IsActive first would collapse the two
        // states maker-checker exists to distinguish into a single "Inactive".
        : approval is MasterDataApprovalState.Draft ? MasterDataState.Draft
        : approval is MasterDataApprovalState.Pending ? MasterDataState.Pending
        : isActive ? MasterDataState.Active
        : MasterDataState.Inactive;
}
