namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// How an approver answered one submission. Null on the request means it is still open - which is
/// the only definition of "in my queue" the module has, so it is deliberately a nullable decision
/// rather than a three-valued enum with a <c>Pending</c> member nothing could index on.
/// </summary>
public enum MasterDataApprovalDecision
{
    /// <summary>Accepted. The record goes live.</summary>
    Approved = 0,

    /// <summary>
    /// Sent back to the steward with a reason. The record returns to draft rather than dying, so
    /// the same record can be corrected and submitted again.
    /// </summary>
    Rejected = 1
}
