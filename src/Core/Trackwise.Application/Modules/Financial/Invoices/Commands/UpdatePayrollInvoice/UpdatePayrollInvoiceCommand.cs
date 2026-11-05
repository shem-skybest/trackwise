using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

[RequiresFeature(TenantFeature.Payroll)]
public class UpdatePayrollInvoiceCommand : ICommand
{
    public Guid Id { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}
