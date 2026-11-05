using FluentValidation;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class AddBillingBatchChargeValidator : AbstractValidator<AddBillingBatchChargeCommand>
{
    public AddBillingBatchChargeValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
