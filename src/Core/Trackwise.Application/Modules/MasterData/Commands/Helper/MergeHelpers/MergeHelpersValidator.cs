using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeHelpersValidator : AbstractValidator<MergeHelpersCommand>
{
    public MergeHelpersValidator()
    {
        RuleFor(i => i.SurvivorId).NotEmpty();
        RuleFor(i => i.LoserId).NotEmpty();

        RuleFor(i => i.LoserId)
            .NotEqual(i => i.SurvivorId)
            .WithMessage("A helper cannot be merged into itself.");

        RuleFor(i => i.Reason).MaximumLength(500);
    }
}
