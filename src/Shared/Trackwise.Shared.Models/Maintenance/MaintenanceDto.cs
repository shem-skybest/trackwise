using Trackwise.Domain.Primitives.Enums.Maintenance;

namespace Trackwise.Shared.Models;

public class MaintenanceRecordDto
{
    public Guid Id { get; set; }
    public Guid TruckId { get; set; }
    public string TruckNumber { get; set; } = string.Empty;
    public Guid? MaintenanceScheduleId { get; set; }

    public MaintenanceType Type { get; set; }
    public string TypeDisplay { get; set; } = string.Empty;
    public VehicleSystem? VehicleSystem { get; set; }
    public string Description { get; set; } = string.Empty;

    public DateTime ServiceDate { get; set; }
    public int? OdometerReading { get; set; }
    public int? EngineHours { get; set; }

    public string? RequisitionNumber { get; set; }
    public string? VendorName { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal LaborCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal TotalCost { get; set; }

    public Guid? PerformedById { get; set; }
    public string? PerformedByName { get; set; }

    public string? Notes { get; set; }
    public List<MaintenancePartDto> Parts { get; set; } = [];
    public List<DocumentDto> Documents { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}

public class MaintenancePartDto
{
    public Guid Id { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PartNumber { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>Every truck's records paid through one request for payment, to check against it.</summary>
public class MaintenanceRequisitionDto
{
    public string RequisitionNumber { get; set; } = string.Empty;
    public DateTime FirstServiceDate { get; set; }
    public DateTime LastServiceDate { get; set; }
    public List<string> VendorNames { get; set; } = [];
    public decimal LaborCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal TotalCost { get; set; }
    public List<MaintenanceRecordDto> Records { get; set; } = [];
}

public class MaintenanceScheduleDto
{
    public Guid Id { get; set; }
    public Guid TruckId { get; set; }
    public string TruckNumber { get; set; } = string.Empty;

    public MaintenanceType Type { get; set; }
    public string TypeDisplay { get; set; } = string.Empty;

    public MaintenanceIntervalType IntervalType { get; set; }
    public int? MileageInterval { get; set; }
    public int? DaysInterval { get; set; }
    public int? EngineHoursInterval { get; set; }

    public int? LastServiceOdometer { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public int? LastServiceEngineHours { get; set; }

    public int? NextServiceOdometer { get; set; }
    public DateTime? NextServiceDate { get; set; }
    public int? NextServiceEngineHours { get; set; }

    public bool IsOverdue { get; set; }
    public int? MilesUntilDue { get; set; }
    public int? DaysUntilDue { get; set; }

    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class MaintenancePartRequest
{
    public required string PartName { get; set; }
    public string? Description { get; set; }
    public string? PartNumber { get; set; }
    public required int Quantity { get; set; }
    public required decimal UnitCost { get; set; }
}
