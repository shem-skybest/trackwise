using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

public class DeleteInvoiceCommand : ICommand, IHaveId
{
    public Guid Id { get; set; }
}
