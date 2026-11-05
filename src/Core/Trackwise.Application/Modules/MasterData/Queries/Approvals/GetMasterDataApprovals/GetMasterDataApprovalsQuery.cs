using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// The approvers' queue, across every entity that needs approval at once.
/// <para>
/// Works for any approvable entity because it reads the request table rather than each entity's own
/// - the same reason <c>GetMasterDataChangeLogQuery</c> is keyed by entity type and id rather than
/// having a foreign key per table. Rates join it by declaring maker-checker on their policy, and
/// this query does not change.
/// </para>
/// </summary>
public class GetMasterDataApprovalsQuery : PagedQuery, IQuery<PagedResult<MasterDataApprovalDto>>
{
    /// <summary>Narrows to one list, e.g. <c>BusinessPartner</c>. Blank means every entity.</summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Whether to return the requests still waiting on somebody, or the ones already decided.
    /// Defaults to the queue, because that is what an approver opens the screen for; the decided
    /// half is the audit trail and is asked for deliberately.
    /// </summary>
    public bool Open { get; set; } = true;
}
