using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums.Maintenance;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Queries;

/// <summary>Search matches the requisition number, vendor, description and truck number.</summary>
public class GetMaintenanceRecordsQuery : SearchableQuery, IQuery<PagedResult<MaintenanceRecordDto>>
{
    public Guid? TruckId { get; set; }
    public MaintenanceType? Type { get; set; }
    public VehicleSystem? VehicleSystem { get; set; }
    public string? RequisitionNumber { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
