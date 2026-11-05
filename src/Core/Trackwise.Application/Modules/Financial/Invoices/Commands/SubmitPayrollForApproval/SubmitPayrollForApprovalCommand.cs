using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

[RequiresFeature(TenantFeature.Payroll)]
public class SubmitPayrollForApprovalCommand : ICommand
{
    public Guid Id { get; set; }
}
