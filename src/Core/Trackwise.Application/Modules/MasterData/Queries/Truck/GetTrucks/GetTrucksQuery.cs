using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

public class GetTrucksQuery : SearchableQuery, IQuery<PagedResult<TruckDto>>
{
    public TruckType? Type { get; set; }

    /// <summary>Any of these types - the list page's multi-select. Combined with <see cref="Type" /> when both are sent.</summary>
    public TruckType[]? Types { get; set; }

    public TruckStatus[]? Statuses { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>Only trucks whose gross vehicle weight hasn't been entered from the CR yet.</summary>
    public bool MissingGrossVehicleWeight { get; set; }

    /// <summary>Trucks scoped to one branch. Records that apply everywhere are always included.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Surfaces soft-deleted records so a steward can review or restore them. Off by default - the
    /// global query filter is what keeps deleted rows out of every other screen.
    /// </summary>
    public bool IncludeDeleted { get; set; }

    /// <summary>
    /// Incremental pull for client systems: return only records touched since this watermark.
    /// </summary>
    public DateTime? UpdatedSince { get; set; }
}
