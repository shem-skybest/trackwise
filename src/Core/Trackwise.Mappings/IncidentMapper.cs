using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class IncidentMapper
{
    [MapperIgnoreSource(nameof(Incident.DomainEvents))]
    [MapperIgnoreSource(nameof(Incident.Witnesses))]
    [MapperIgnoreSource(nameof(Incident.ThirdParties))]
    [MapperIgnoreSource(nameof(Incident.Documents))]
    [MapperIgnoreSource(nameof(Incident.UpdatedAt))]
    [MapperIgnoreSource(nameof(Incident.UpdatedBy))]
    [MapperIgnoreTarget(nameof(IncidentDto.TripNumber))]
    [MapperIgnoreTarget(nameof(IncidentDto.CustomerName))]
    [MapperIgnoreTarget(nameof(IncidentDto.TruckNumber))]
    [MapperIgnoreTarget(nameof(IncidentDto.DriverName))]
    [MapperIgnoreTarget(nameof(IncidentDto.HelperName))]
    [MapperIgnoreTarget(nameof(IncidentDto.SecondHelperName))]
    [MapperIgnoreTarget(nameof(IncidentDto.Repairs))]
    [MapperIgnoreTarget(nameof(IncidentDto.Rescues))]
    [MapperIgnoreTarget(nameof(IncidentDto.Attachments))]
    [MapperIgnoreTarget(nameof(IncidentDto.TotalCost))]
    private static partial IncidentDto MapIncident(Incident entity);

    /// <summary>
    /// Names come from lookups the caller batched, for the reasons <see cref="TripMapper" /> gives:
    /// a navigation read per row is an N+1, and soft-deleted trucks and crew would read as null.
    /// </summary>
    public static IncidentDto ToDto(
        this Incident entity,
        IncidentRefNames names,
        IReadOnlyList<IncidentRepairDto>? repairs = null,
        IReadOnlyList<TripRescueDto>? rescues = null,
        IReadOnlyList<IncidentAttachmentDto>? attachments = null)
    {
        var dto = MapIncident(entity);
        repairs ??= [];
        return dto with
        {
            TripNumber = names.TripNumber,
            CustomerName = names.CustomerName,
            TruckNumber = names.TruckNumber,
            DriverName = names.DriverName,
            HelperName = names.HelperName,
            SecondHelperName = names.SecondHelperName,
            Repairs = repairs,
            Rescues = rescues ?? [],
            Attachments = attachments ?? [],
            TotalCost = repairs.Sum(r => r.TotalCost) + (entity.ThirdPartyCompensation ?? 0)
        };
    }

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial IncidentRepairDto ToIncidentRepairDto(this MaintenanceRecord record);

    public static IncidentAttachmentDto ToIncidentAttachmentDto(this IncidentDocument document, string? uploadedBy) => new()
    {
        Id = document.Id,
        Type = document.Type,
        FileName = document.OriginalFileName,
        ContentType = document.ContentType,
        FileSizeBytes = document.FileSizeBytes,
        UploadedAt = document.CreatedAt,
        UploadedBy = uploadedBy
    };

    [MapperIgnoreSource(nameof(TripRescue.DomainEvents))]
    [MapperIgnoreTarget(nameof(TripRescueDto.TripNumber))]
    [MapperIgnoreTarget(nameof(TripRescueDto.TruckNumber))]
    [MapperIgnoreTarget(nameof(TripRescueDto.DriverName))]
    [MapperIgnoreTarget(nameof(TripRescueDto.HelperName))]
    [MapperIgnoreTarget(nameof(TripRescueDto.SecondHelperName))]
    [MapperIgnoreTarget(nameof(TripRescueDto.IncidentCaseNumber))]
    [MapperIgnoreTarget(nameof(TripRescueDto.StopIds))]
    private static partial TripRescueDto MapRescue(TripRescue rescue);

    public static TripRescueDto ToDto(this TripRescue rescue, RescueRefNames names, IReadOnlyList<Guid> stopIds)
    {
        var dto = MapRescue(rescue);
        return dto with
        {
            TripNumber = names.TripNumber,
            TruckNumber = names.TruckNumber,
            DriverName = names.DriverName,
            HelperName = names.HelperName,
            SecondHelperName = names.SecondHelperName,
            IncidentCaseNumber = names.IncidentCaseNumber,
            StopIds = stopIds
        };
    }
}
