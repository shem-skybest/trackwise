using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Queries;

/// <summary>
/// Every active driver and helper on one day: their status and the truck they're planned on. Drivers
/// first, then helpers, each by name.
/// </summary>
public class GetCrewDayQuery : IQuery<Result<IList<CrewDayMemberDto>>>
{
    /// <summary>The day to show; the tenant's today when omitted.</summary>
    public DateOnly? Date { get; set; }
}
