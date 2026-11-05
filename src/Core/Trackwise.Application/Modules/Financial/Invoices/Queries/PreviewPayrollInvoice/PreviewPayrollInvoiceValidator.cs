using FluentValidation;

namespace Trackwise.Application.Modules.Financial.Invoices.Queries;

internal sealed class PreviewPayrollInvoiceValidator : AbstractValidator<PreviewPayrollInvoiceQuery>
{
    public PreviewPayrollInvoiceValidator()
    {
        RuleFor(i => i.PeriodStart).LessThan(i => i.PeriodEnd);
        RuleFor(i => i.EmployeeId).NotEmpty();
    }
}
