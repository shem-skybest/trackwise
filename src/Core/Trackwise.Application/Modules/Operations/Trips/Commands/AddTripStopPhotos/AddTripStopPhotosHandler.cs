using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Abstractions.Storage;
using Trackwise.Application.Modules.Common.Constants;
using Trackwise.Mappings;
using Trackwise.Application.Utilities;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class AddTripStopPhotosHandler(
    ITenantUnitOfWork tenantUow,
    IBlobStorageService blobStorage,
    ICurrentUserService currentUser,
    ILogger<AddTripStopPhotosHandler> logger)
    : IAppRequestHandler<AddTripStopPhotosCommand, Result<List<TripStopPhotoDto>>>
{
    public async Task<Result<List<TripStopPhotoDto>>> Handle(AddTripStopPhotosCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);
        if (trip is null)
        {
            return Result<List<TripStopPhotoDto>>.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        var stopIds = req.StopIds.Distinct().ToList();
        var stops = trip.Stops.Where(s => stopIds.Contains(s.Id)).OrderBy(s => s.Order).ToList();
        if (stops.Count != stopIds.Count)
        {
            return Result<List<TripStopPhotoDto>>.Fail("Some of those stops aren't on this trip.");
        }

        if (stops.Select(s => s.Type).Distinct().Count() > 1)
        {
            return Result<List<TripStopPhotoDto>>.Fail("A photo covers one warehouse visit or one drop, not both.");
        }

        // Document.UploadedBy is an employee, so only office staff with an employee record can upload.
        var userId = currentUser.GetUserId();
        var employee = userId is null ? null : await tenantUow.Repository<Employee>().GetByIdAsync(userId.Value, ct);
        if (employee is null)
        {
            return Result<List<TripStopPhotoDto>>.Fail("Only staff with an employee record can add photos.");
        }

        var type = stops[0].Type == TripStopType.PickUp
            ? DocumentType.PickupInspection
            : DocumentType.ProofOfDelivery;
        var capturedAt = DateTime.UtcNow;
        var added = new List<TripStopPhotoDto>();
        var index = 0;

        foreach (var photo in req.Photos)
        {
            // One blob per photo, shared by a document per stop: a two-load warehouse visit shows the
            // same picture on both loads without storing it twice.
            var fileName = BlobPathHelper.GenerateUniqueFileName(photo.FileName, index++);
            var blobPath = BlobPathHelper.GetLoadBlobPath(stops[0].LoadId, "photos", fileName);
            await blobStorage.UploadAsync(
                BlobConstants.DocumentsContainerName, blobPath, photo.Content, photo.ContentType, ct);

            var documents = stops.Select(stop => DeliveryDocument.Create(
                    fileName,
                    photo.FileName,
                    photo.ContentType,
                    photo.FileSizeBytes,
                    blobPath,
                    BlobConstants.DocumentsContainerName,
                    type,
                    stop.LoadId,
                    employee.Id,
                    capturedAt: capturedAt,
                    tripStopId: stop.Id))
                .ToList();

            foreach (var document in documents)
            {
                await tenantUow.Repository<DeliveryDocument>().AddAsync(document, ct);
            }

            added.Add(documents.ToTripStopPhotoDto(employee.Name));
        }

        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Added {PhotoCount} {Type} photo(s) to {StopCount} stop(s) of trip '{TripName}' (ID: {TripId})",
            added.Count, type, stops.Count, trip.Name, trip.Id);

        return Result<List<TripStopPhotoDto>>.Ok(added);
    }
}
