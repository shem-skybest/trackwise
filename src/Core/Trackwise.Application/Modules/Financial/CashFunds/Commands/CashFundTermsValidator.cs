using FluentValidation;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

internal abstract class CashFundTermsValidator<T> : AbstractValidator<T>
    where T : CashFundTermsCommand
{
    protected CashFundTermsValidator()
    {
        RuleFor(i => i.CustodianId).NotEmpty().WithMessage("Pick who holds the fund.");
        RuleFor(i => i.PeriodStart).NotEmpty();
        RuleFor(i => i.PeriodEnd)
            .GreaterThanOrEqualTo(i => i.PeriodStart)
            .WithMessage("The period can't end before it starts.");
        RuleFor(i => i.Amount).GreaterThanOrEqualTo(0);
        RuleFor(i => i.AdvanceReference).MaximumLength(64);
        RuleFor(i => i.Notes).MaximumLength(500);
    }
}
