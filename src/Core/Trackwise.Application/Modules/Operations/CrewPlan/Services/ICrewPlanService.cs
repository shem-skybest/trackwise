using Trackwise.Application.Abstractions.Common;
using Trackwise.Domain.Entities;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Services;

/// <summary>
/// Reads the dispatcher's daily crew plan and guards it against putting one person on two trucks, or
/// someone who isn't working that day on any.
/// </summary>
public interface ICrewPlanService : IApplicationService
{
    /// <summary>The tenant's calendar date at <paramref name="utc" /> - the day a plan row belongs to.</summary>
    DateOnly LocalDate(DateTime utc);

    /// <summary>The truck's plan row for <paramref name="date" />, or null when the dispatcher hasn't planned it.</summary>
    Task<CrewAssignment?> FindAsync(Guid truckId, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// A message naming the clash when anyone on <paramref name="assignment" />'s crew is already on
    /// another truck's crew that day; null when there is none.
    /// </summary>
    Task<string?> FindConflictAsync(CrewAssignment assignment, CancellationToken ct = default);

    /// <summary>
    /// A message naming the first of <paramref name="personIds" /> whose status on
    /// <paramref name="date" /> keeps them off a truck (absent, on leave, suspended); null when
    /// everyone can work.
    /// </summary>
    Task<string?> FindUnavailableAsync(DateOnly date, IReadOnlyCollection<Guid> personIds, CancellationToken ct = default);

    /// <summary>The day a trip runs: its earliest requested pickup, or today, in the tenant's calendar.</summary>
    DateOnly TripDay(IEnumerable<Load> loads);
}
