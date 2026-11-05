using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Common.Constants;
using Trackwise.Application.Utilities;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.Storage;

namespace Trackwise.Application.Modules.Financial.Expenses.Commands;

internal sealed class UploadExpenseReceiptHandler(IBlobStorageService blobStorageService)
    : IAppRequestHandler<UploadExpenseReceiptCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UploadExpenseReceiptCommand req, CancellationToken ct)
    {
        try
        {
            var uniqueFileName = BlobPathHelper.GenerateUniqueFileName(req.FileName);
            var blobPath = BlobPathHelper.GetReceiptBlobPath(uniqueFileName);

            await blobStorageService.UploadAsync(
                BlobConstants.ReceiptsContainerName,
                blobPath,
                req.FileContent,
                req.ContentType,
                ct);

            return Result<string>.Ok(blobPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Failed to upload receipt: {ex.Message}");
        }
    }
}
