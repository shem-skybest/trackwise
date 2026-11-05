using FluentValidation;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

internal sealed class ReviewCashFundExpensesValidator : AbstractValidator<ReviewCashFundExpensesCommand>
{
    public ReviewCashFundExpensesValidator()
    {
        RuleFor(i => i.ExpenseIds).NotEmpty().WithMessage("Pick the receipts to review.");
        RuleFor(i => i.Reason)
            .NotEmpty()
            .When(i => !i.Accept)
            .WithMessage("Say why the receipts are disallowed.");
        RuleFor(i => i.Reason).MaximumLength(500);
    }
}
