using Trackwise.Domain.Entities;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits;

/// <summary>Which paper counts, and when to warn about it. Pure, so the reminder logic is testable without a database.</summary>
public static class VehiclePermitRules
{
    /// <summary>Days before expiry at which a reminder goes out; 0 is the "it has expired" reminder.</summary>
    public static readonly int[] ReminderThresholds = [60, 30, 7, 0];

    /// <summary>
    /// The ids of the current papers: per vehicle and type, the one expiring last. A renewal is a new
    /// record, so an older record of the same type is history, and its lapse is not a problem.
    /// </summary>
    public static HashSet<Guid> CurrentIds(IEnumerable<VehiclePermit> permits) =>
        permits
            .GroupBy(p => (p.TruckId, p.TrailerId, p.Type))
            .Select(g => g.MaxBy(p => p.ExpiresOn)!.Id)
            .ToHashSet();

    /// <summary>
    /// The threshold a reminder is due for, or null when none is. Each threshold fires once: a paper
    /// 25 days out is due the 30-day reminder unless that (or a later one) already went.
    /// </summary>
    public static int? DueThreshold(int daysUntilExpiry, int? lastThresholdSent)
    {
        if (daysUntilExpiry > ReminderThresholds[0])
        {
            return null;
        }

        var threshold = ReminderThresholds.Where(t => daysUntilExpiry <= t).Min();
        return lastThresholdSent is { } last && last <= threshold ? null : threshold;
    }
}
