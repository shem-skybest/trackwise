using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Storage;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class DeleteTripStopPhotoHandler(
    ITenantUnitOfWork tenantUow,
    IBlobStorageService blobStorage,
    ILogger<DeleteTripStopPhotoHandler> logger)
    : IAppRequestHandler<DeleteTripStopPhotoCommand, Result>
{
    public async Task<Result> Handle(DeleteTripStopPhotoCommand req, CancellationToken ct)
    {
        var documents = tenantUow.Repository<DeliveryDocument>();
        var photo = await documents.GetByIdAsync(req.PhotoId, ct);
        var stopIds = await tenantUow.Repository<TripStop>().Query()
            .Where(s => s.TripId == req.TripId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (photo?.TripStopId is not { } stopId || !stopIds.Contains(stopId))
        {
            return Result.Fail("Could not find that photo on this trip.");
        }

        var sameBlob = await documents.Query()
            .Where(d => d.BlobPath == photo.BlobPath && d.BlobContainer == photo.BlobContainer)
            .ToListAsync(ct);

        foreach (var document in sameBlob)
        {
            documents.Delete(document);
        }

        await tenantUow.SaveChangesAsync(ct);

        try
        {
            await blobStorage.DeleteAsync(photo.BlobContainer, photo.BlobPath, ct);
        }
        catch (Exception ex)
        {
            // The documents are gone, so nothing points at the file any more - an orphaned blob, not a broken photo.
            logger.LogError(ex, "Failed to delete photo blob {BlobPath} for trip {TripId}", photo.BlobPath, req.TripId);
        }

        logger.LogInformation(
            "Deleted photo {PhotoId} ({DocumentCount} document(s)) from trip {TripId}",
            req.PhotoId, sameBlob.Count, req.TripId);

        return Result.Ok();
    }
}
