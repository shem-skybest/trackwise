using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Storage;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

internal sealed class DeleteIncidentHandler(
    ITenantUnitOfWork tenantUow,
    IBlobStorageService blobStorage,
    ILogger<DeleteIncidentHandler> logger)
    : IAppRequestHandler<DeleteIncidentCommand, Result>
{
    public async Task<Result> Handle(DeleteIncidentCommand req, CancellationToken ct)
    {
        var incident = await tenantUow.Repository<Incident>().GetByIdAsync(req.Id, ct);
        if (incident is null)
        {
            return Result.Fail($"Could not find the incident with ID '{req.Id}'.");
        }

        try
        {
            incident.EnsureDeletable();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        // The attachments go with the incident (cascade); their files are removed once it's gone.
        var files = incident.Documents.Select(d => (d.BlobContainer, d.BlobPath)).ToList();
        tenantUow.Repository<Incident>().Delete(incident);
        await tenantUow.SaveChangesAsync(ct);

        foreach (var (container, path) in files)
        {
            try
            {
                await blobStorage.DeleteAsync(container, path, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete blob {BlobPath} of deleted incident {IncidentId}", path, req.Id);
            }
        }

        return Result.Ok();
    }
}
