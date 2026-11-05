using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Abstractions.Documents;
using Trackwise.Application.Abstractions.Storage;
using Trackwise.Application.Modules.IdentityAccess.Tenants;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Queries;

internal sealed class GetBillingBatchPdfHandler(
    ITenantUnitOfWork tenantUow,
    IBillingBatchPdfService pdfService,
    IBlobStorageService blobStorage,
    ICurrentUserService currentUser)
    : IAppRequestHandler<GetBillingBatchPdfQuery, Result<BillingBatchPdf>>
{
    public async Task<Result<BillingBatchPdf>> Handle(GetBillingBatchPdfQuery req, CancellationToken ct)
    {
        var batch = await tenantUow.Repository<BillingBatch>().GetByIdAsync(req.Id, ct);
        if (batch is null)
        {
            return Result<BillingBatchPdf>.Fail($"Could not find billing batch with ID '{req.Id}'.");
        }

        var dto = await BillingBatchReader.ToDtoAsync(tenantUow, batch, ct);
        var tenant = tenantUow.GetCurrentTenant();
        var logo = await TenantLogoReader.ReadAsync(blobStorage, tenant, ct);
        var pdf = pdfService.GenerateBillingBatchPdf(dto, tenant, logo, currentUser.GetUserName());
        return Result<BillingBatchPdf>.Ok(new BillingBatchPdf(batch.ReferenceNumber, pdf));
    }
}
