namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Whether changes to an entity need a second pair of eyes before they go live.
/// </summary>
public enum MasterDataApprovalMode
{
    /// <summary>Saved straight to <see cref="MasterDataState.Active"/>. Low-impact lookups.</summary>
    None = 0,

    /// <summary>
    /// Draft -> Pending -> Active, where the approver may not be the submitter. Required for
    /// partners and rates, where a bad record costs real money.
    /// </summary>
    MakerChecker = 1
}
