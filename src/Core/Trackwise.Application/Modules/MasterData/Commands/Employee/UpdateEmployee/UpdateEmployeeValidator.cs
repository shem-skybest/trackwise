using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(i => i.Salary).GreaterThanOrEqualTo(0M).When(i => i.Salary.HasValue);
        RuleFor(i => i.JobTitle).MaximumLength(100);
        RuleFor(i => i.EmploymentType).IsInEnum();
        RuleFor(i => i.HomeUnit).IsInEnum();

        // Retiring an employee things still depend on is allowed, but never silently - the reason is
        // what a reviewer reads when those dependents turn out to have mattered.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deactivating an employee that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents && !i.IsActive);
    }
}
