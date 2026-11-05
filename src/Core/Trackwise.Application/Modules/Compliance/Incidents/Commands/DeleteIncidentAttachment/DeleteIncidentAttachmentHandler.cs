using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Storage;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums.Safety;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

internal sealed class DeleteIncidentAttachmentHandler(
    ITenantUnitOfWork tenantUow,
    IBlobStorageService blobStorage,
    ILogger<DeleteIncidentAttachmentHandler> logger)
    : IAppRequestHandler<DeleteIncidentAttachmentCommand, Result<IncidentDto>>
{
    public async Task<Result<IncidentDto>> Handle(DeleteIncidentAttachmentCommand req, CancellationToken ct)
    {
        var incident = await tenantUow.Repository<Incident>().GetByIdAsync(req.IncidentId, ct);
        var document = await tenantUow.Repository<IncidentDocument>().GetByIdAsync(req.AttachmentId, ct);
        if (incident is null || document is null || document.IncidentId != incident.Id)
        {
            return Result<IncidentDto>.Fail("Could not find that file on this incident.");
        }

        if (incident.Status == IncidentStatus.Closed)
        {
            return Result<IncidentDto>.Fail("This incident is closed and can't be changed.");
        }

        tenantUow.Repository<IncidentDocument>().Delete(document);
        await tenantUow.SaveChangesAsync(ct);

        try
        {
            await blobStorage.DeleteAsync(document.BlobContainer, document.BlobPath, ct);
        }
        catch (Exception ex)
        {
            // The document is gone, so nothing points at the file any more - an orphaned blob, not a broken attachment.
            logger.LogError(ex, "Failed to delete blob {BlobPath} of incident {IncidentId}", document.BlobPath, incident.Id);
        }

        return Result<IncidentDto>.Ok(await IncidentReader.ToDetailsDtoAsync(tenantUow, incident, ct));
    }
}
