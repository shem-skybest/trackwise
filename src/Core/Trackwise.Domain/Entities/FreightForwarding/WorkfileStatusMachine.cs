using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Encapsulates the legal workfile status transitions. Booking deliberately has no equivalent and
/// accepts any transition; a workfile is handed between ops, management and accounting, so an
/// illegal jump here would skip an approval rather than just look untidy.
/// </summary>
public static class WorkfileStatusMachine
{
    private static readonly Dictionary<WorkfileStatus, WorkfileStatus[]> AllowedTransitions = new()
    {
        [WorkfileStatus.Open] = [WorkfileStatus.ManagerReview, WorkfileStatus.Cancelled],
        // Back to Open when an approver disapproves and ops has to rework the file.
        [WorkfileStatus.ManagerReview] =
            [WorkfileStatus.Open, WorkfileStatus.AccountingReview, WorkfileStatus.Cancelled],
        [WorkfileStatus.AccountingReview] = [WorkfileStatus.Closed],
        [WorkfileStatus.Closed] = [],
        [WorkfileStatus.Cancelled] = []
    };

    public static bool CanTransition(WorkfileStatus current, WorkfileStatus target) =>
        AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(target);

    public static WorkfileStatus[] GetAllowedTransitions(WorkfileStatus current) =>
        AllowedTransitions.TryGetValue(current, out var allowed) ? allowed : [];

    /// <summary>Statuses in which ops may still edit the header, containers and draft charges.</summary>
    public static bool IsEditable(WorkfileStatus status) => status is WorkfileStatus.Open;
}
