using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Queries;

/// <summary>The crew plan for one day: a row per active truck, planned or not, ordered by unit number.</summary>
public class GetCrewPlanQuery : IQuery<Result<IList<CrewPlanRowDto>>>
{
    /// <summary>The day to show; the tenant's today when omitted.</summary>
    public DateOnly? Date { get; set; }

    /// <summary>Just this truck's row, e.g. to fill a new trip's crew.</summary>
    public Guid? TruckId { get; set; }

    public TruckType? TruckType { get; set; }
    public Guid? BranchId { get; set; }
}
