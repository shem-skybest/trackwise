using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class DvirMapper
{
    /// <summary>
    /// Reads <c>TruckNumber</c> off the lazy navigation - fine for a single-row handler, wrong for a
    /// list (N+1) and wrong once the truck has been soft-deleted (the global filter reads it as
    /// null). List handlers should call the overload below instead, passing a number resolved past
    /// the filter via <c>TruckRefResolver</c>.
    /// </summary>
    public static DvirReportDto ToDto(this DvirReport entity) => entity.ToDto(entity.Truck?.Number);

    public static DvirReportDto ToDto(this DvirReport entity, string? truckNumber)
    {
        return new DvirReportDto
        {
            Id = entity.Id,
            TruckId = entity.TruckId,
            TruckNumber = truckNumber ?? string.Empty,
            DriverId = entity.DriverId,
            DriverName = entity.Driver?.GetFullName() ?? string.Empty,
            Type = entity.Type,
            Status = entity.Status,
            InspectionDate = entity.InspectionDate,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            OdometerReading = entity.OdometerReading,
            HasDefects = entity.HasDefects,
            HasDriverSignature = !string.IsNullOrEmpty(entity.DriverSignature),
            DriverNotes = entity.DriverNotes,
            ReviewedById = entity.ReviewedById,
            ReviewedByName = entity.ReviewedBy?.GetFullName(),
            ReviewedAt = entity.ReviewedAt,
            HasMechanicSignature = !string.IsNullOrEmpty(entity.MechanicSignature),
            MechanicNotes = entity.MechanicNotes,
            DefectsCorrected = entity.DefectsCorrected,
            TripId = entity.TripId,
            Defects = entity.Defects.Select(d => d.ToDto()).ToList(),
            Photos = entity.Photos.Select(p => p.ToDto()).ToList(),
            CreatedAt = entity.CreatedAt
        };
    }

    public static DvirDefectDto ToDto(this DvirDefect entity)
    {
        return new DvirDefectDto
        {
            Id = entity.Id,
            Category = entity.Category,
            CategoryDisplay = entity.Category.GetDescription(),
            Description = entity.Description,
            Severity = entity.Severity,
            SeverityDisplay = entity.Severity.GetDescription(),
            IsCorrected = entity.IsCorrected,
            CorrectionNotes = entity.CorrectionNotes,
            CorrectedAt = entity.CorrectedAt,
            CorrectedById = entity.CorrectedById,
            CorrectedByName = entity.CorrectedBy?.GetFullName()
        };
    }
}
