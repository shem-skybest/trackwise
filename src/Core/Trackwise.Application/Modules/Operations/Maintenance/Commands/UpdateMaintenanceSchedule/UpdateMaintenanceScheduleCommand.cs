using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Maintenance;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

/// <summary>Changes the interval or corrects the last service; the truck and service type stay.</summary>
[RequiresFeature(TenantFeature.Maintenance)]
public record UpdateMaintenanceScheduleCommand : ICommand<Result<MaintenanceScheduleDto>>, IMaintenanceScheduleFields
{
    public required Guid Id { get; set; }
    public required MaintenanceIntervalType IntervalType { get; set; }
    public int? MileageInterval { get; set; }
    public int? DaysInterval { get; set; }
    public int? EngineHoursInterval { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public int? LastServiceOdometer { get; set; }
    public int? LastServiceEngineHours { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}
