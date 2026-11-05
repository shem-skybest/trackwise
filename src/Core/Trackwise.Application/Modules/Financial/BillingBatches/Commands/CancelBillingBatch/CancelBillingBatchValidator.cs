using FluentValidation;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class CancelBillingBatchValidator : AbstractValidator<CancelBillingBatchCommand>
{
    public CancelBillingBatchValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
