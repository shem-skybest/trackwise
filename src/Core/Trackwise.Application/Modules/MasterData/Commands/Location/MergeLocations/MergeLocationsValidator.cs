using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeLocationsValidator : AbstractValidator<MergeLocationsCommand>
{
    public MergeLocationsValidator()
    {
        RuleFor(i => i.SurvivorId).NotEmpty();
        RuleFor(i => i.LoserId).NotEmpty();

        RuleFor(i => i.LoserId)
            .NotEqual(i => i.SurvivorId)
            .WithMessage("A location cannot be merged into itself.");

        RuleFor(i => i.Reason).MaximumLength(500);
    }
}
