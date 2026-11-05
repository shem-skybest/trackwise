using FluentValidation;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class UnlockBillingBatchValidator : AbstractValidator<UnlockBillingBatchCommand>
{
    public UnlockBillingBatchValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
