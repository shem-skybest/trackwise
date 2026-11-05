using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

public class GetCommoditiesQuery : SearchableQuery, IQuery<PagedResult<CommodityDto>>
{
    public bool? IsHazardous { get; set; }
    public bool? RequiresTemperatureControl { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>Commodities scoped to one branch. Records that apply everywhere are always included.</summary>
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
