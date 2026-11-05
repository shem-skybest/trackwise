using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.IdentityAccess.Employees;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.Documents;
using Trackwise.Application.Abstractions.Storage;
using Trackwise.Application.Modules.IdentityAccess.Tenants;

namespace Trackwise.Application.Modules.Financial.Invoices.Queries;

internal sealed class GetPayrollPayStubPdfHandler(
    ITenantUnitOfWork tenantUow,
    IPayrollPayStubService pdfService,
    IBlobStorageService blobStorage)
    : IAppRequestHandler<GetPayrollPayStubPdfQuery, Result<InvoicePdfResult>>
{
    public async Task<Result<InvoicePdfResult>> Handle(GetPayrollPayStubPdfQuery request, CancellationToken ct)
    {
        var payroll = await tenantUow.Repository<PayrollInvoice>().GetByIdAsync(request.PayrollInvoiceId, ct);

        if (payroll is null)
        {
            return Result<InvoicePdfResult>.Fail($"Could not find a payroll invoice with ID {request.PayrollInvoiceId}");
        }

        // A pay stub is historical - a retired employee's own past stub must still name them rather
        // than the PDF's null-safe fallback ("N/A") silently masking whose stub it is. Assigning the
        // navigation directly is harmless here: nothing in this handler calls SaveChangesAsync.
        var employees = await EmployeeRefResolver.ResolveEntitiesAsync(tenantUow, [payroll.EmployeeId], ct);

        if (employees.TryGetValue(payroll.EmployeeId, out var employee))
        {
            payroll.Employee = employee;
        }

        var tenant = tenantUow.GetCurrentTenant();
        var logo = await TenantLogoReader.ReadAsync(blobStorage, tenant, ct);
        var pdfBytes = pdfService.GeneratePayStubPdf(payroll, tenant, logo);
        var fileName = $"PayStub-{payroll.Number}.pdf";

        return Result<InvoicePdfResult>.Ok(new InvoicePdfResult(pdfBytes, fileName));
    }
}
