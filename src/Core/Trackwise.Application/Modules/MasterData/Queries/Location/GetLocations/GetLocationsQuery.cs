using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

public class GetLocationsQuery : SearchableQuery, IQuery<PagedResult<LocationDto>>
{
    public LocationType? Type { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>
    /// Only the locations inside this zone. Distinct from <see cref="BranchId"/>: one is where the
    /// location sits, the other is who maintains it.
    /// </summary>
    public Guid? ParentLocationId { get; set; }

    /// <summary>Locations scoped to one branch. Records that apply everywhere are always included.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>One client's sites. Exact unless <see cref="IncludeShared"/> is set.</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// With <see cref="CustomerId"/>, also return the sites no client owns - what a load form
    /// offers, since a shared cold store is as valid a drop as the client's own outlet. The
    /// client's own list leaves it off.
    /// </summary>
    public bool IncludeShared { get; set; }

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
