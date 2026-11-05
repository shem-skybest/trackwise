using Trackwise.Domain.Core;

namespace Trackwise.Domain.Entities.Maintenance;

/// <summary>
/// Parts used in a maintenance service
/// </summary>
public class MaintenancePart : Entity, ITenantEntity
{
    public Guid MaintenanceRecordId { get; set; }
    public virtual MaintenanceRecord MaintenanceRecord { get; set; } = null!;

    public required string PartName { get; set; }

    /// <summary>The specifics beyond the part's name - brand, size, fitment ("7.50/16 tube type").</summary>
    public string? Description { get; set; }

    public string? PartNumber { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public static MaintenancePart Create(Guid recordId, MaintenancePartSpec spec) => new()
    {
        MaintenanceRecordId = recordId,
        PartName = spec.PartName.Trim(),
        Description = string.IsNullOrWhiteSpace(spec.Description) ? null : spec.Description.Trim(),
        PartNumber = string.IsNullOrWhiteSpace(spec.PartNumber) ? null : spec.PartNumber.Trim(),
        Quantity = spec.Quantity,
        UnitCost = spec.UnitCost,
        TotalCost = spec.Quantity * spec.UnitCost
    };
}
