using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Queries;

public class GetLoadInvoicePdfQuery : IQuery<Result<InvoicePdfResult>>
{
    public Guid InvoiceId { get; set; }
}

public record InvoicePdfResult(byte[] PdfBytes, string FileName);
