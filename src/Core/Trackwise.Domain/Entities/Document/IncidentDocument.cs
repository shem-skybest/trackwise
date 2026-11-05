using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>A photo, police report or other paper attached to an incident.</summary>
public class IncidentDocument : Document
{
    public override DocumentOwnerType OwnerType { get; protected set; } = DocumentOwnerType.Incident;

    public Guid IncidentId { get; private set; }
    public virtual Incident Incident { get; private set; } = null!;

    public static IncidentDocument Create(
        string fileName,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string blobPath,
        string blobContainer,
        DocumentType type,
        Guid incidentId,
        Guid uploadedById)
    {
        return new IncidentDocument
        {
            FileName = fileName,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            BlobPath = blobPath,
            BlobContainer = blobContainer,
            Type = type,
            IncidentId = incidentId,
            UploadedById = uploadedById,
            Status = DocumentStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    protected override void OnStatusChanged(DocumentStatus newStatus)
    {
    }
}
