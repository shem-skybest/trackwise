using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Maintenance;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

[RequiresFeature(TenantFeature.Maintenance)]
public record CreateMaintenanceScheduleCommand : ICommand<Result<MaintenanceScheduleDto>>, IMaintenanceScheduleFields
{
    public required Guid TruckId { get; set; }
    public required MaintenanceType Type { get; set; }
    public required MaintenanceIntervalType IntervalType { get; set; }
    public int? MileageInterval { get; set; }
    public int? DaysInterval { get; set; }
    public int? EngineHoursInterval { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public int? LastServiceOdometer { get; set; }
    public int? LastServiceEngineHours { get; set; }
    public string? Notes { get; set; }
}
