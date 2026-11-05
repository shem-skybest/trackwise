using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Queries;

public class GetInvoiceByIdQuery : IQuery<Result<InvoiceDto>>, IHaveId
{
    public Guid Id { get; set; }
}
