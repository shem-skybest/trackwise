using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

public class GetHelpersQuery : SearchableQuery, IQuery<PagedResult<HelperDto>>
{
    public bool? IsActive { get; set; }

    /// <summary>
    /// Only active helpers who can be put on a truck this day: anyone whose crew status for it is
    /// absent, on leave, suspended, training or lent out is left out. For the crew pickers.
    /// </summary>
    public DateOnly? AvailableOn { get; set; }

    /// <summary>Helpers scoped to one branch. Records that apply everywhere are always included.</summary>
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
