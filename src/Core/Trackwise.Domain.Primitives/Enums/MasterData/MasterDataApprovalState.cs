namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Where a record governed by <see cref="MasterDataApprovalMode.MakerChecker"/> sits between being
/// written and being live.
/// <para>
/// The one part of <see cref="MasterDataState"/> that has to be <b>stored</b> rather than derived.
/// Every other state reads off a flag the record already carries - <c>IsActive</c>,
/// <c>DeletedAt</c>, <c>MergedIntoId</c> - but nothing on a record distinguishes "typed in and not
/// finished" from "submitted and waiting", and both look identical to an inactive record. Storing
/// it is what lets an approver's queue exist at all.
/// </para>
/// <para>
/// There is deliberately no <c>Rejected</c> member. A rejected submission goes back to
/// <see cref="Draft"/> with the reviewer's note attached to the approval request, because the
/// record itself is then exactly what it was before it was submitted: someone's unfinished work.
/// A fourth state would be a dead end a steward has to be given a way out of.
/// </para>
/// </summary>
public enum MasterDataApprovalState
{
    /// <summary>
    /// Being prepared. Editable by its steward, invisible to consumers, and never
    /// <c>IsActive</c> - only approval sets that.
    /// </summary>
    Draft = 0,

    /// <summary>Submitted and sitting in an approver's queue. Still not live.</summary>
    Pending = 1,

    /// <summary>
    /// A second person accepted it and the record went live. Terminal in the ordinary sense: an
    /// approved record is edited, deactivated and deleted like any other master record.
    /// </summary>
    Approved = 2
}
