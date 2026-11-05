using FluentValidation;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Commands;

internal sealed class SetCrewStatusValidator : AbstractValidator<SetCrewStatusCommand>
{
    /// <summary>Two months covers a long leave; anything longer is a roster change, not a day status.</summary>
    public const int MaxDays = 62;

    public SetCrewStatusValidator()
    {
        RuleFor(i => i.PersonId).NotEmpty();
        RuleFor(i => i.Date).NotEmpty();
        RuleFor(i => i.Status).IsInEnum();
        RuleFor(i => i.Notes).MaximumLength(500);

        RuleFor(i => i.UntilDate)
            .GreaterThanOrEqualTo(i => i.Date)
            .WithMessage("The last day can't be before the first.")
            .Must((cmd, until) => until!.Value.DayNumber - cmd.Date.DayNumber < MaxDays)
            .WithMessage($"Set at most {MaxDays} days at once.")
            .When(i => i.UntilDate.HasValue);
    }
}
