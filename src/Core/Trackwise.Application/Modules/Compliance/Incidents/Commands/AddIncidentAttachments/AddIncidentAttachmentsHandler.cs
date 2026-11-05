using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Abstractions.Storage;
using Trackwise.Application.Modules.Common.Constants;
using Trackwise.Application.Utilities;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums.Safety;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

internal sealed class AddIncidentAttachmentsHandler(
    ITenantUnitOfWork tenantUow,
    IBlobStorageService blobStorage,
    ICurrentUserService currentUser)
    : IAppRequestHandler<AddIncidentAttachmentsCommand, Result<IncidentDto>>
{
    public async Task<Result<IncidentDto>> Handle(AddIncidentAttachmentsCommand req, CancellationToken ct)
    {
        var incident = await tenantUow.Repository<Incident>().GetByIdAsync(req.IncidentId, ct);
        if (incident is null)
        {
            return Result<IncidentDto>.Fail($"Could not find the incident with ID '{req.IncidentId}'.");
        }

        if (incident.Status == IncidentStatus.Closed)
        {
            return Result<IncidentDto>.Fail("This incident is closed and can't be changed.");
        }

        // Document.UploadedBy is an employee, so only office staff with an employee record can attach.
        var userId = currentUser.GetUserId();
        var employee = userId is null ? null : await tenantUow.Repository<Employee>().GetByIdAsync(userId.Value, ct);
        if (employee is null)
        {
            return Result<IncidentDto>.Fail("Only staff with an employee record can attach files.");
        }

        var index = 0;
        foreach (var file in req.Files)
        {
            var fileName = BlobPathHelper.GenerateUniqueFileName(file.FileName, index++);
            var blobPath = BlobPathHelper.GetOwnerDocumentBlobPath("incidents", incident.Id, fileName);
            await blobStorage.UploadAsync(
                BlobConstants.DocumentsContainerName, blobPath, file.Content, file.ContentType, ct);

            var document = IncidentDocument.Create(
                fileName,
                file.FileName,
                file.ContentType,
                file.FileSizeBytes,
                blobPath,
                BlobConstants.DocumentsContainerName,
                req.Type,
                incident.Id,
                employee.Id);
            await tenantUow.Repository<IncidentDocument>().AddAsync(document, ct);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result<IncidentDto>.Ok(await IncidentReader.ToDetailsDtoAsync(tenantUow, incident, ct));
    }
}
