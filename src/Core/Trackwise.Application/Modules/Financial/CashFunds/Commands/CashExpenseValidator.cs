using FluentValidation;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

internal abstract class CashExpenseValidator<T> : AbstractValidator<T>
    where T : CashExpenseCommand
{
    protected CashExpenseValidator()
    {
        RuleFor(i => i.SpentOn).NotEmpty();
        RuleFor(i => i.Category).IsInEnum();
        RuleFor(i => i.Description).NotEmpty().WithMessage("Say what the expense was for.").MaximumLength(200);
        RuleFor(i => i.Amount).GreaterThan(0);
        RuleFor(i => i.ReceiptNumber).MaximumLength(64);
    }
}
