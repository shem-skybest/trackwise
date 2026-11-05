using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

[RequiresFeature(TenantFeature.Payroll)]
public class BatchApprovePayrollCommand : ICommand
{
    public required List<Guid> Ids { get; set; }
    public string? Notes { get; set; }
}
