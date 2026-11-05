using FluentValidation;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class CreateBillingBatchValidator : AbstractValidator<CreateBillingBatchCommand>
{
    public CreateBillingBatchValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.PeriodEnd)
            .GreaterThanOrEqualTo(x => x.PeriodStart)
            .WithMessage("The period can't end before it starts.");
        RuleFor(x => x.PeriodEnd)
            .LessThanOrEqualTo(x => x.PeriodStart.AddDays(62))
            .WithMessage("A billing period can't be longer than two months.");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
