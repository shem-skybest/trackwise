using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class MaintenanceMapper
{
    /// <summary>
    /// Reads <c>TruckNumber</c> off the lazy navigation - fine for a single-row handler, wrong for a
    /// list (N+1) and wrong once the truck has been soft-deleted (the global filter reads it as
    /// null). List and historical-display handlers should call the overload below instead, passing
    /// a number resolved past the filter via <c>TruckRefResolver</c>.
    /// </summary>
    public static MaintenanceRecordDto ToDto(this MaintenanceRecord entity) =>
        entity.ToDto(entity.Truck?.Number);

    public static MaintenanceRecordDto ToDto(this MaintenanceRecord entity, string? truckNumber)
    {
        return new MaintenanceRecordDto
        {
            Id = entity.Id,
            TruckId = entity.TruckId,
            TruckNumber = truckNumber ?? string.Empty,
            MaintenanceScheduleId = entity.MaintenanceScheduleId,
            Type = entity.MaintenanceType,
            TypeDisplay = entity.MaintenanceType.GetDescription(),
            VehicleSystem = entity.VehicleSystem,
            Description = entity.Description ?? string.Empty,
            ServiceDate = entity.ServiceDate,
            OdometerReading = entity.OdometerReading,
            EngineHours = entity.EngineHours,
            RequisitionNumber = entity.RequisitionNumber,
            VendorName = entity.VendorName,
            InvoiceNumber = entity.InvoiceNumber,
            LaborCost = entity.LaborCost,
            PartsCost = entity.PartsCost,
            TotalCost = entity.TotalCost,
            PerformedById = entity.PerformedById,
            PerformedByName = entity.PerformedBy?.GetFullName(),
            Notes = entity.WorkPerformed,
            Parts = entity.Parts.Select(p => p.ToDto()).ToList(),
            Documents = entity.Documents.Select(d => d.ToDto()).ToList(),
            CreatedAt = entity.CreatedAt
        };
    }

    public static MaintenancePartDto ToDto(this MaintenancePart entity)
    {
        return new MaintenancePartDto
        {
            Id = entity.Id,
            PartName = entity.PartName,
            Description = entity.Description,
            PartNumber = entity.PartNumber,
            Quantity = entity.Quantity,
            UnitCost = entity.UnitCost,
            TotalCost = entity.TotalCost
        };
    }

    /// <summary>See the <see cref="MaintenanceRecord"/> overload's doc comment - the same trade-off applies.</summary>
    public static MaintenanceScheduleDto ToDto(this MaintenanceSchedule entity, int? currentOdometer = null) =>
        entity.ToDto(entity.Truck?.Number, currentOdometer);

    public static MaintenanceScheduleDto ToDto(
        this MaintenanceSchedule entity, string? truckNumber, int? currentOdometer)
    {
        var isOverdue = false;
        int? milesUntilDue = null;
        int? daysUntilDue = null;

        if (entity.NextDueMileage.HasValue && currentOdometer.HasValue)
        {
            milesUntilDue = entity.NextDueMileage.Value - currentOdometer.Value;
            if (milesUntilDue < 0) isOverdue = true;
        }

        if (entity.NextDueDate.HasValue)
        {
            daysUntilDue = entity.DaysUntilDue(DateTime.UtcNow);
            if (daysUntilDue < 0) isOverdue = true;
        }

        return new MaintenanceScheduleDto
        {
            Id = entity.Id,
            TruckId = entity.TruckId,
            TruckNumber = truckNumber ?? string.Empty,
            Type = entity.MaintenanceType,
            TypeDisplay = entity.MaintenanceType.GetDescription(),
            IntervalType = entity.IntervalType,
            MileageInterval = entity.MileageInterval,
            DaysInterval = entity.DaysInterval,
            EngineHoursInterval = entity.EngineHoursInterval,
            LastServiceOdometer = entity.LastServiceMileage,
            LastServiceDate = entity.LastServiceDate,
            LastServiceEngineHours = entity.LastServiceEngineHours,
            NextServiceOdometer = entity.NextDueMileage,
            NextServiceDate = entity.NextDueDate,
            NextServiceEngineHours = entity.NextDueEngineHours,
            IsOverdue = isOverdue,
            MilesUntilDue = milesUntilDue,
            DaysUntilDue = daysUntilDue,
            IsActive = entity.IsActive,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt
        };
    }
}
