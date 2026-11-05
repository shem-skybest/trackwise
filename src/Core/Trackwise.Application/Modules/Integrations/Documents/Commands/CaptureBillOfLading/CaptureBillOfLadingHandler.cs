using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Common.Constants;
using Trackwise.Application.Modules.Operations.Loads;
using Trackwise.Application.Utilities;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Storage;

namespace Trackwise.Application.Modules.Integrations.Documents.Commands;

internal sealed class CaptureBillOfLadingHandler(
    ITenantUnitOfWork tenantUow,
    IBlobStorageService blobStorage,
    ILogger<CaptureBillOfLadingHandler> logger)
    : IAppRequestHandler<CaptureBillOfLadingCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CaptureBillOfLadingCommand req, CancellationToken ct)
    {
        // Verify load exists
        var load = await tenantUow.Repository<Load>().GetByIdAsync(req.LoadId, ct);
        if (load is null)
        {
            return Result<Guid>.Fail($"Load with ID '{req.LoadId}' not found");
        }

        // Verify employee exists
        var employee = await tenantUow.Repository<Employee>().GetByIdAsync(req.CapturedById, ct);
        if (employee is null)
        {
            return Result<Guid>.Fail($"Employee with ID '{req.CapturedById}' not found");
        }

        if (!await DriverLoadAccess.CanActOnAsync(tenantUow, req.CapturedById, load.Id, ct))
        {
            return Result<Guid>.Fail(DriverLoadAccess.NotYourLoad);
        }

        // A photo taken at a warehouse visit belongs to every load picked up there, as on the crew report.
        IReadOnlyList<(Guid LoadId, Guid? TripStopId)> targets = [(load.Id, null)];
        if (req.TripStopId.HasValue)
        {
            var tripStop = await tenantUow.Repository<TripStop>().GetByIdAsync(req.TripStopId.Value, ct);
            if (tripStop is null || tripStop.LoadId != load.Id)
            {
                return Result<Guid>.Fail($"Trip stop with ID '{req.TripStopId}' not found on this load");
            }

            targets = tripStop.Trip.GetSameVisitPickups(tripStop.Id)
                .Select(s => (s.LoadId, (Guid?)s.Id))
                .ToList();
        }

        var uploadedDocIds = new List<Guid>();
        var capturedAt = DateTime.UtcNow;
        string? signatureBlobPath = null;

        try
        {
            // Upload signature if provided
            if (!string.IsNullOrEmpty(req.SignatureBase64))
            {
                var signatureBytes = Convert.FromBase64String(req.SignatureBase64);
                var signatureFileName = BlobPathHelper.GenerateSignatureFileName();
                signatureBlobPath = BlobPathHelper.GetLoadBlobPath(req.LoadId, "bol", signatureFileName);

                using var signatureStream = new MemoryStream(signatureBytes);
                await blobStorage.UploadAsync(
                    BlobConstants.DocumentsContainerName,
                    signatureBlobPath,
                    signatureStream,
                    "image/png",
                    ct);
            }

            // Upload photos
            var photoIndex = 0;
            foreach (var photo in req.Photos)
            {
                var uniqueFileName = BlobPathHelper.GenerateUniqueFileName(photo.FileName, photoIndex++);
                var blobPath = BlobPathHelper.GetLoadBlobPath(req.LoadId, "bol", uniqueFileName);

                await blobStorage.UploadAsync(
                    BlobConstants.DocumentsContainerName,
                    blobPath,
                    photo.Content,
                    photo.ContentType,
                    ct);

                // One blob, shared by a document per load of the visit.
                foreach (var target in targets)
                {
                    var doc = DeliveryDocument.Create(
                        uniqueFileName,
                        photo.FileName,
                        photo.ContentType,
                        photo.FileSizeBytes,
                        blobPath,
                        BlobConstants.DocumentsContainerName,
                        DocumentType.BillOfLading,
                        target.LoadId,
                        req.CapturedById,
                        req.RecipientName,
                        signatureBlobPath,
                        req.Latitude,
                        req.Longitude,
                        capturedAt,
                        target.TripStopId,
                        req.Notes);

                    await tenantUow.Repository<DeliveryDocument>().AddAsync(doc, ct);
                    uploadedDocIds.Add(doc.Id);
                }
            }

            // If no photos but we have signature/shipper info, create a single BOL record
            if (req.Photos.Count == 0 && (!string.IsNullOrEmpty(req.SignatureBase64) || !string.IsNullOrEmpty(req.RecipientName)))
            {
                var bolFileName = BlobPathHelper.GeneratePlaceholderFileName("bol");
                var blobPath = BlobPathHelper.GetLoadBlobPath(req.LoadId, "bol", bolFileName);

                // Create a placeholder document for the signature-only BOL
                var doc = DeliveryDocument.Create(
                    bolFileName,
                    "bill_of_lading.json",
                    "application/json",
                    0,
                    blobPath,
                    BlobConstants.DocumentsContainerName,
                    DocumentType.BillOfLading,
                    req.LoadId,
                    req.CapturedById,
                    req.RecipientName,
                    signatureBlobPath,
                    req.Latitude,
                    req.Longitude,
                    capturedAt,
                    req.TripStopId,
                    req.Notes);

                await tenantUow.Repository<DeliveryDocument>().AddAsync(doc, ct);
                uploadedDocIds.Add(doc.Id);
            }

            var changes = await tenantUow.SaveChangesAsync(ct);
            if (changes > 0 && uploadedDocIds.Count > 0)
            {
                logger.LogInformation(
                    "BOL captured for load {LoadId}: {PhotoCount} photos, signature: {HasSignature}",
                    req.LoadId, req.Photos.Count, !string.IsNullOrEmpty(req.SignatureBase64));

                return Result<Guid>.Ok(uploadedDocIds.First());
            }

            return Result<Guid>.Fail("No documents were created");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to capture BOL for load {LoadId}", req.LoadId);

            // Cleanup any uploaded blobs on failure
            foreach (var docId in uploadedDocIds)
            {
                // Note: In production, you might want to track blob paths for cleanup
            }

            return Result<Guid>.Fail($"Failed to capture bill of lading: {ex.Message}");
        }
    }
}
