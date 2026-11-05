using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Storage;

namespace Trackwise.Application.Modules.Integrations.Documents.Commands;

internal sealed class DeleteDocumentHandler(
    ITenantUnitOfWork tenantUow,
    IBlobStorageService blobStorageService,
    ILogger<DeleteDocumentHandler> logger)
    : IAppRequestHandler<DeleteDocumentCommand, Result>
{
    public async Task<Result> Handle(
        DeleteDocumentCommand req, CancellationToken ct)
    {
        var documentRepo = tenantUow.Repository<Document>();
        var document = await documentRepo.GetByIdAsync(req.DocumentId, ct);

        if (document is null)
        {
            return Result.Fail($"Could not find document with ID '{req.DocumentId}'");
        }

        try
        {
            // Store blob info before deletion
            var blobContainer = document.BlobContainer;
            var blobPath = document.BlobPath;

            // Hard delete the document from database
            documentRepo.Delete(document);
            var changes = await tenantUow.SaveChangesAsync(ct);

            if (changes > 0)
            {
                logger.LogInformation(
                    "Document deleted: {DocumentId}, BlobPath: {BlobPath}",
                    req.DocumentId, blobPath);
                // A crew-report photo is one blob shared by every load of the warehouse visit.
                var stillShared = await documentRepo.Query()
                    .AnyAsync(d => d.BlobPath == blobPath && d.BlobContainer == blobContainer, ct);
                if (!stillShared)
                {
                    _ = DeleteBlobAsync(blobContainer, blobPath, ct);
                }
                return Result.Ok();
            }

            logger.LogWarning("Failed to delete document {DocumentId} from database", req.DocumentId);
            return Result.Fail("Failed to delete document");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete document {DocumentId}", req.DocumentId);
            return Result.Fail($"Failed to delete document: {ex.Message}");
        }
    }

    private Task DeleteBlobAsync(string blobContainer, string blobPath, CancellationToken ct)
    {
        return Task.Run(async () =>
        {
            try
            {
                await blobStorageService.DeleteAsync(blobContainer, blobPath, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete blob {BlobPath} from container {BlobContainer}", blobPath,
                    blobContainer);
            }
        }, ct);
    }
}
