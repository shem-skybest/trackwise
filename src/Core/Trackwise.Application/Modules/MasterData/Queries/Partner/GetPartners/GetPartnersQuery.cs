using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// The steward's list of business partners. Unlike the read-only <c>/business-partners</c> feed,
/// this one shows drafts and pending records - they are the steward's own work in progress, and a
/// list that hid them would leave a submission with nowhere to be seen from.
/// </summary>
public class GetPartnersQuery : SearchableQuery, IQuery<PagedResult<BusinessPartnerDto>>
{
    /// <summary>Restricts results to partners that can act in this capacity.</summary>
    public BusinessPartnerRole? Role { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>
    /// Narrows to one stage of maker-checker. <c>Pending</c> is the approver's queue for this
    /// entity alone; the cross-entity queue is <c>GetMasterDataApprovalsQuery</c>.
    /// </summary>
    public MasterDataApprovalState? ApprovalState { get; set; }

    /// <summary>Partners scoped to one branch. Records that apply everywhere are always included.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Surfaces soft-deleted records so a steward can review or restore them. Off by default - the
    /// global query filter is what keeps deleted rows out of every other screen.
    /// </summary>
    public bool IncludeDeleted { get; set; }

    /// <summary>
    /// Incremental pull for client systems: return only records touched since this watermark.
    /// Callers should subtract an overlap window rather than passing their last sync time exactly,
    /// or a record written mid-sync is missed forever.
    /// </summary>
    public DateTime? UpdatedSince { get; set; }
}
