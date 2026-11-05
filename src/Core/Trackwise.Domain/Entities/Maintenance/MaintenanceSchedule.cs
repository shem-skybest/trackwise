using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums.Maintenance;

namespace Trackwise.Domain.Entities.Maintenance;

/// <summary>
/// Recurring maintenance schedule for a truck
/// </summary>
public class MaintenanceSchedule : AuditableEntity, ITenantEntity
{
    public Guid TruckId { get; set; }
    public virtual Truck Truck { get; set; } = null!;

    public required MaintenanceType MaintenanceType { get; set; }
    public required MaintenanceIntervalType IntervalType { get; set; }

    /// <summary>
    /// Interval in the tenant's distance unit (for distance-based)
    /// </summary>
    public int? MileageInterval { get; set; }

    /// <summary>
    /// Interval in days (for time-based)
    /// </summary>
    public int? DaysInterval { get; set; }

    /// <summary>
    /// Interval in engine hours
    /// </summary>
    public int? EngineHoursInterval { get; set; }

    // Last service tracking
    public int? LastServiceMileage { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public int? LastServiceEngineHours { get; set; }

    // Next due calculations
    public int? NextDueMileage { get; set; }
    public DateTime? NextDueDate { get; set; }
    public int? NextDueEngineHours { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    /// <summary>Past its due day - a service due today is due, not yet overdue.</summary>
    public bool IsOverdue => DaysUntilDue(DateTime.UtcNow) < 0;

    public bool IsDueSoon => !IsOverdue && DaysUntilDue(DateTime.UtcNow) <= 7;

    /// <summary>Whole days from <paramref name="now" />'s date to the due date; null with no due date.</summary>
    public int? DaysUntilDue(DateTime now) => NextDueDate is { } due ? (due.Date - now.Date).Days : null;

    /// <summary>
    /// Sets how often the service comes round, keeping only the intervals the type uses, and works
    /// out when it's next due. A time interval with no service on record is due at once: nobody
    /// knows when it was last done.
    /// </summary>
    /// <exception cref="InvalidOperationException">An interval the type needs is missing.</exception>
    public void SetInterval(MaintenanceIntervalType type, int? distance, int? days, int? engineHours)
    {
        distance = distance > 0 ? distance : null;
        days = days > 0 ? days : null;
        engineHours = engineHours > 0 ? engineHours : null;

        (MileageInterval, DaysInterval, EngineHoursInterval) = type switch
        {
            MaintenanceIntervalType.Mileage => (distance ?? throw Missing("a distance"), null, null),
            MaintenanceIntervalType.TimeBased => (null, days ?? throw Missing("a number of days"), null),
            MaintenanceIntervalType.EngineHours => (null, null, engineHours ?? throw Missing("a number of engine hours")),
            _ => new[] { distance, days, engineHours }.Count(v => v.HasValue) >= 2
                ? (distance, days, engineHours)
                : throw new InvalidOperationException(
                    "A combined interval needs at least two of distance, days and engine hours.")
        };

        IntervalType = type;
        Recalculate();
    }

    /// <summary>
    /// Moves the schedule on from a service done on <paramref name="date" />. A service older than
    /// the last one on record changes nothing, so back-filling history can't set the schedule back.
    /// </summary>
    public void RecordService(DateTime date, int? odometer, int? engineHours)
    {
        if (LastServiceDate is { } last && date < last)
        {
            return;
        }

        LastServiceDate = date;
        LastServiceMileage = odometer ?? LastServiceMileage;
        LastServiceEngineHours = engineHours ?? LastServiceEngineHours;
        Recalculate();
    }

    private void Recalculate()
    {
        NextDueDate = (DaysInterval, LastServiceDate) switch
        {
            (null, _) => null,
            (_, null) => DateTime.UtcNow.Date,
            ({ } days, { } last) => last.AddDays(days)
        };
        NextDueMileage = MileageInterval + LastServiceMileage;
        NextDueEngineHours = EngineHoursInterval + LastServiceEngineHours;
    }

    private static InvalidOperationException Missing(string what) =>
        new($"This interval needs {what} greater than zero.");
}
