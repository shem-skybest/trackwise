using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums.Maintenance;

public enum MaintenanceIntervalType
{
    [Description("Mileage Based")]
    Mileage,

    TimeBased,
    EngineHours,

    [Description("Combined (First to Occur)")]
    Combined
}
