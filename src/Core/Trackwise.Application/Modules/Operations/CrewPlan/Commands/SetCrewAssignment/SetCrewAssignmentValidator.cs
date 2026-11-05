using FluentValidation;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Commands;

internal sealed class SetCrewAssignmentValidator : AbstractValidator<SetCrewAssignmentCommand>
{
    public SetCrewAssignmentValidator()
    {
        RuleFor(i => i.Date).NotEmpty();
        RuleFor(i => i.TruckId).NotEmpty();
        RuleFor(i => i.UnitStatus).IsInEnum();
        RuleFor(i => i.Notes).MaximumLength(500);

        RuleFor(i => i.Notes)
            .NotEmpty()
            .When(i => i.UnitStatus == TruckDayStatus.Other)
            .WithMessage("Say why the unit is off the road.");

        RuleFor(i => i.SecondHelperId)
            .NotEqual(i => i.HelperId)
            .When(i => i.SecondHelperId.HasValue)
            .WithMessage("Pick a different second helper.");
    }
}
