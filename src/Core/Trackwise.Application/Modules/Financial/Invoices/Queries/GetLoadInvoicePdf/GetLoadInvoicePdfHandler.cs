using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.Documents;
using Trackwise.Application.Abstractions.Storage;
using Trackwise.Application.Modules.IdentityAccess.Tenants;

namespace Trackwise.Application.Modules.Financial.Invoices.Queries;

internal sealed class GetLoadInvoicePdfHandler(
    ITenantUnitOfWork tenantUow,
    IInvoicePdfService pdfService,
    IBlobStorageService blobStorage)
    : IAppRequestHandler<GetLoadInvoicePdfQuery, Result<InvoicePdfResult>>
{
    public async Task<Result<InvoicePdfResult>> Handle(GetLoadInvoicePdfQuery request, CancellationToken ct)
    {
        var invoice = await tenantUow.Repository<LoadInvoice>().GetByIdAsync(request.InvoiceId, ct);

        if (invoice is null)
        {
            return Result<InvoicePdfResult>.Fail($"Could not find an invoice with ID {request.InvoiceId}");
        }

        var tenant = tenantUow.GetCurrentTenant();
        var logo = await TenantLogoReader.ReadAsync(blobStorage, tenant, ct);
        var pdfBytes = pdfService.GenerateLoadInvoicePdf(invoice, tenant, logo);
        var fileName = $"Invoice-{invoice.Number}.pdf";

        return Result<InvoicePdfResult>.Ok(new InvoicePdfResult(pdfBytes, fileName));
    }
}
