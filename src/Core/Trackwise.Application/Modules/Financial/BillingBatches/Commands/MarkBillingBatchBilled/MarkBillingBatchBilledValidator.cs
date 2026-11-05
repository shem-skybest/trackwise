using FluentValidation;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class MarkBillingBatchBilledValidator : AbstractValidator<MarkBillingBatchBilledCommand>
{
    public MarkBillingBatchBilledValidator()
    {
        RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(100);
    }
}
