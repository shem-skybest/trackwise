using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

/// <summary>
/// Creates payroll invoices for multiple employees at once.
/// </summary>
[RequiresFeature(TenantFeature.Payroll)]
public record BatchCreatePayrollInvoicesCommand : ICommand<Result<BatchCreatePayrollInvoicesResult>>
{
    public required List<Guid> EmployeeIds { get; set; }
    public required DateTime PeriodStart { get; set; }
    public required DateTime PeriodEnd { get; set; }
}
