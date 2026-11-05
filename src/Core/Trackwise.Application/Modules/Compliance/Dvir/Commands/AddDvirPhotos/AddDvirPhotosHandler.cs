using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Abstractions.Storage;
using Trackwise.Application.Modules.Common.Constants;
using Trackwise.Application.Utilities;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Safety;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Commands;

internal sealed class AddDvirPhotosHandler(
    ITenantUnitOfWork tenantUow,
    IBlobStorageService blobStorage,
    ICurrentUserService currentUser)
    : IAppRequestHandler<AddDvirPhotosCommand, Result<DvirReportDto>>
{
    public async Task<Result<DvirReportDto>> Handle(AddDvirPhotosCommand req, CancellationToken ct)
    {
        var report = await tenantUow.Repository<DvirReport>().GetByIdAsync(req.ReportId, ct);
        if (report is null)
        {
            return Result<DvirReportDto>.Fail("DVIR report not found.");
        }

        var callerId = currentUser.GetUserId();
        if (!await DvirReportAccess.CanFileForAsync(tenantUow, callerId, report.DriverId, ct))
        {
            return Result<DvirReportDto>.Fail("You can only add photos to your own DVIR.");
        }

        if (report.Status != DvirStatus.Draft)
        {
            return Result<DvirReportDto>.Fail("Photos can only be added before the report is submitted.");
        }

        // The photos are truck documents too, so they also show on the truck's own file.
        var index = 0;
        foreach (var file in req.Files)
        {
            var fileName = BlobPathHelper.GenerateUniqueFileName(file.FileName, index++);
            var blobPath = BlobPathHelper.GetOwnerDocumentBlobPath("dvir", report.Id, fileName);
            await blobStorage.UploadAsync(
                BlobConstants.DocumentsContainerName, blobPath, file.Content, file.ContentType, ct);

            var document = TruckDocument.Create(
                fileName,
                file.FileName,
                file.ContentType,
                file.FileSizeBytes,
                blobPath,
                BlobConstants.DocumentsContainerName,
                DocumentType.Photo,
                report.TruckId,
                callerId!.Value,
                $"Truck check photo ({report.InspectionDate:yyyy-MM-dd})");
            report.Photos.Add(document);
            await tenantUow.Repository<TruckDocument>().AddAsync(document, ct);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result<DvirReportDto>.Ok(report.ToDto());
    }
}
