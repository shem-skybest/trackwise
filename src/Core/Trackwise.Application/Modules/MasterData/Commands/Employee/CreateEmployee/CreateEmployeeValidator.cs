using FluentValidation;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeValidator()
    {
        RuleFor(i => i.UserId).NotEmpty();
        RuleFor(i => i.Salary).GreaterThanOrEqualTo(0M);
        When(i => i.SalaryType != SalaryType.None,
            () => RuleFor(i => i.Salary).GreaterThan(0M));
    }
}
