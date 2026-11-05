using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Financial.Payroll.Commands;

public class PayPayrollInvoiceCommand : ICommand
{
    public Guid InvoiceId { get; set; }
}
