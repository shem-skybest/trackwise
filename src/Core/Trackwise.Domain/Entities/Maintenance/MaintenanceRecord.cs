using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums.Maintenance;

namespace Trackwise.Domain.Entities.Maintenance;

/// <summary>
/// Service/maintenance record for a truck
/// </summary>
public class MaintenanceRecord : AuditableEntity, ITenantEntity
{
    public Guid TruckId { get; set; }
    public virtual Truck Truck { get; set; } = null!;

    /// <summary>
    /// Link to schedule if this was scheduled maintenance
    /// </summary>
    public Guid? MaintenanceScheduleId { get; set; }
    public virtual MaintenanceSchedule? MaintenanceSchedule { get; set; }

    public required MaintenanceType MaintenanceType { get; set; }

    /// <summary>The incident the repair was for, e.g. the accident that broke the windshield.</summary>
    public Guid? IncidentId { get; set; }

    /// <summary>Which part of the truck the work was on, where known.</summary>
    public VehicleSystem? VehicleSystem { get; set; }

    public required DateTime ServiceDate { get; set; }

    /// <summary>In the tenant's distance unit. Often not noted - Cold Chain's repair board never had it.</summary>
    public int? OdometerReading { get; set; }

    public int? EngineHours { get; set; }

    private string? requisitionNumber;

    /// <summary>
    /// The request for payment the parts and labour were paid through - nSFS's <c>RFP-RFV</c>
    /// (<c>RFV26-001517-M</c>). One requisition often covers several trucks, each its own record.
    /// </summary>
    public string? RequisitionNumber
    {
        get => requisitionNumber;
        set => requisitionNumber = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    public string? VendorName { get; set; }
    public string? VendorAddress { get; set; }
    public string? InvoiceNumber { get; set; }

    public decimal LaborCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal TotalCost { get; set; }

    public string? Description { get; set; }
    public string? WorkPerformed { get; set; }

    /// <summary>
    /// If performed internally
    /// </summary>
    public Guid? PerformedById { get; set; }
    public virtual Employee? PerformedBy { get; set; }

    public virtual List<TruckDocument> Documents { get; set; } = [];
    public virtual List<MaintenancePart> Parts { get; set; } = [];

    /// <summary>
    /// Replaces the part lines and recomputes the costs: parts cost is the lines' sum, or - for a
    /// service logged from one invoice total with no breakdown - the amount given. The returned parts
    /// are new rows: the caller
    /// must delete the old ones and add these through the repository, since a tracked record's
    /// collection alone would save them as updates to rows that don't exist.
    /// </summary>
    public IReadOnlyList<MaintenancePart> ReplaceParts(IEnumerable<MaintenancePartSpec> specs, decimal laborCost, decimal partsCostWithoutLines)
    {
        Parts.Clear();
        foreach (var spec in specs)
        {
            Parts.Add(MaintenancePart.Create(Id, spec));
        }

        LaborCost = laborCost;
        PartsCost = Parts.Count > 0 ? Parts.Sum(p => p.TotalCost) : partsCostWithoutLines;
        TotalCost = LaborCost + PartsCost;
        return Parts;
    }
}
