using Trackwise.Domain.Primitives.Enums.Maintenance;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

/// <summary>The service record fields create and update share, so they're checked and applied once.</summary>
public interface IMaintenanceRecordFields
{
    Guid TruckId { get; }

    /// <summary>The schedule this service belongs to. Left empty, the truck's one active schedule of the same type is used.</summary>
    Guid? MaintenanceScheduleId { get; }

    MaintenanceType Type { get; }
    VehicleSystem? VehicleSystem { get; }
    string Description { get; }
    DateTime ServiceDate { get; }
    int? OdometerReading { get; }
    int? EngineHours { get; }
    string? RequisitionNumber { get; }
    string? VendorName { get; }
    string? InvoiceNumber { get; }
    decimal LaborCost { get; }

    /// <summary>Used only when there are no part lines; otherwise parts cost is their sum.</summary>
    decimal PartsCost { get; }

    Guid? PerformedById { get; }
    string? Notes { get; }
    List<MaintenancePartRequest> Parts { get; }
}
