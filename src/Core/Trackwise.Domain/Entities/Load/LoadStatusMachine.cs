using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Encapsulates the state machine logic for load status transitions.
/// </summary>
public static class LoadStatusMachine
{
    private static readonly Dictionary<LoadStatus, LoadStatus[]> AllowedTransitions = new()
    {
        [LoadStatus.Draft] = [LoadStatus.Dispatched, LoadStatus.Cancelled],
        [LoadStatus.Dispatched] = [LoadStatus.PickedUp, LoadStatus.Cancelled],
        [LoadStatus.PickedUp] =
        [
            LoadStatus.Delivered, LoadStatus.Cancelled, LoadStatus.FoulTrip, LoadStatus.CancelledByClient
        ],
        [LoadStatus.Delivered] = [],
        [LoadStatus.Cancelled] = [],
        [LoadStatus.FoulTrip] = [],
        [LoadStatus.CancelledByClient] = []
    };

    /// <summary>
    /// Checks if transitioning from the current status to the target status is allowed.
    /// </summary>
    public static bool CanTransition(LoadStatus current, LoadStatus target)
    {
        return AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(target);
    }

    /// <summary>
    /// Gets all valid next statuses from the current status.
    /// </summary>
    public static LoadStatus[] GetAllowedTransitions(LoadStatus current)
    {
        return AllowedTransitions.TryGetValue(current, out var allowed) ? allowed : [];
    }

    /// <summary>
    /// Whether this status is a terminal drop outcome - successful or not. A trip's drop-offs are
    /// "resolved" once every one of them lands here, not only once every one is
    /// <see cref="LoadStatus.Delivered"/>.
    /// </summary>
    public static bool IsResolved(LoadStatus status) =>
        AllowedTransitions.TryGetValue(status, out var allowed) && allowed.Length == 0;

    /// <summary>
    /// Applies the status transition and returns the updated state.
    /// </summary>
    /// <param name="current">Current load status.</param>
    /// <param name="target">Target status to transition to.</param>
    /// <param name="force">If true, bypass transition validation.</param>
    /// <param name="timestamps">Current timestamp values.</param>
    /// <param name="at">
    /// When the change actually happened, for a reported time entered after the fact. Defaults to now.
    /// </param>
    /// <returns>Updated state with new status and timestamps.</returns>
    /// <exception cref="InvalidOperationException">Thrown for invalid transitions when force is false.</exception>
    public static LoadStatusState Apply(
        LoadStatus current,
        LoadStatus target,
        bool force,
        LoadStatusTimestamps timestamps,
        DateTime? at = null)
    {
        if (target == current)
        {
            return new LoadStatusState(current, timestamps);
        }

        if (!force && !CanTransition(current, target))
        {
            throw new InvalidOperationException($"Cannot change load status from '{current}' to '{target}'.");
        }

        var stamp = at ?? DateTime.UtcNow;

        return target switch
        {
            LoadStatus.Draft => new LoadStatusState(
                target,
                timestamps),

            LoadStatus.Dispatched => new LoadStatusState(
                target,
                timestamps with
                {
                    DispatchedAt = timestamps.DispatchedAt ?? stamp,
                    PickedUpAt = null,
                    DeliveredAt = null,
                    CancelledAt = null
                }),

            LoadStatus.PickedUp => new LoadStatusState(
                target,
                timestamps with { PickedUpAt = timestamps.PickedUpAt ?? stamp }),

            LoadStatus.Delivered => new LoadStatusState(
                target,
                timestamps with { DeliveredAt = timestamps.DeliveredAt ?? stamp }),

            // Foul Trip / Cancelled by Client both reuse CancelledAt - both are "did not deliver"
            // outcomes, same as a plain Cancelled, just reported as their own status rather than
            // a free-text reason on top of it.
            LoadStatus.Cancelled or LoadStatus.FoulTrip or LoadStatus.CancelledByClient => new LoadStatusState(
                target,
                timestamps with { CancelledAt = timestamps.CancelledAt ?? stamp }),

            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
        };
    }
}

/// <summary>
/// Represents the timestamps associated with load status changes.
/// </summary>
public record LoadStatusTimestamps(
    DateTime? DispatchedAt,
    DateTime? PickedUpAt,
    DateTime? DeliveredAt,
    DateTime? CancelledAt);

/// <summary>
/// Represents the result of a status transition.
/// </summary>
public record LoadStatusState(
    LoadStatus Status,
    LoadStatusTimestamps Timestamps);
