using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Queries;

public class GetPayrollPayStubPdfQuery : IQuery<Result<InvoicePdfResult>>
{
    public Guid PayrollInvoiceId { get; set; }
}
