using FluentValidation;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

internal sealed class ApprovePayrollInvoiceValidator : AbstractValidator<ApprovePayrollInvoiceCommand>
{
    public ApprovePayrollInvoiceValidator()
    {
        RuleFor(i => i.Notes).MaximumLength(1000);
    }
}
