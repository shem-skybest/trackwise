using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Maintenance;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

/// <summary>Replaces the whole record, part lines included - send every line that should remain.</summary>
[RequiresFeature(TenantFeature.Maintenance)]
public record UpdateMaintenanceRecordCommand : ICommand<Result<MaintenanceRecordDto>>, IMaintenanceRecordFields
{
    public required Guid Id { get; set; }
    public required Guid TruckId { get; set; }
    public Guid? MaintenanceScheduleId { get; set; }
    public required MaintenanceType Type { get; set; }
    public VehicleSystem? VehicleSystem { get; set; }
    public required string Description { get; set; }
    public required DateTime ServiceDate { get; set; }
    public int? OdometerReading { get; set; }
    public int? EngineHours { get; set; }
    public string? RequisitionNumber { get; set; }
    public string? VendorName { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal LaborCost { get; set; }
    public decimal PartsCost { get; set; }
    public Guid? PerformedById { get; set; }
    public string? Notes { get; set; }
    public List<MaintenancePartRequest> Parts { get; set; } = [];
}
