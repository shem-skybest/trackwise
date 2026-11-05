using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeContainerTypesValidator : AbstractValidator<MergeContainerTypesCommand>
{
    public MergeContainerTypesValidator()
    {
        RuleFor(i => i.SurvivorId).NotEmpty();
        RuleFor(i => i.LoserId).NotEmpty();

        RuleFor(i => i.LoserId)
            .NotEqual(i => i.SurvivorId)
            .WithMessage("A container type cannot be merged into itself.");

        RuleFor(i => i.Reason).MaximumLength(500);
    }
}
