using Trackwise.Domain.Primitives.Enums.Maintenance;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

/// <summary>The schedule fields create and update share.</summary>
public interface IMaintenanceScheduleFields
{
    MaintenanceIntervalType IntervalType { get; }

    /// <summary>In the tenant's distance unit.</summary>
    int? MileageInterval { get; }

    int? DaysInterval { get; }
    int? EngineHoursInterval { get; }

    /// <summary>When it was last done, if known. Left empty, a time-based schedule is due at once.</summary>
    DateTime? LastServiceDate { get; }

    int? LastServiceOdometer { get; }
    int? LastServiceEngineHours { get; }
    string? Notes { get; }
}
