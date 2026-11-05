using FluentValidation;

namespace Trackwise.Application.Modules.Financial.Expenses.Commands;

internal sealed class CreateCompanyExpenseValidator : ExpenseValidatorBase<CreateCompanyExpenseCommand>
{
    public CreateCompanyExpenseValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
    }
}
