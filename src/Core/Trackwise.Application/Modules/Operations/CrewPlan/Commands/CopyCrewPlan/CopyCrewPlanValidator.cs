using FluentValidation;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Commands;

internal sealed class CopyCrewPlanValidator : AbstractValidator<CopyCrewPlanCommand>
{
    public CopyCrewPlanValidator()
    {
        RuleFor(i => i.FromDate).NotEmpty();
        RuleFor(i => i.ToDate).NotEmpty().NotEqual(i => i.FromDate).WithMessage("Copy to a different day.");
    }
}
