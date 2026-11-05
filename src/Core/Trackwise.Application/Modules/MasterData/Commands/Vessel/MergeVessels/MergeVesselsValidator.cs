using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeVesselsValidator : AbstractValidator<MergeVesselsCommand>
{
    public MergeVesselsValidator()
    {
        RuleFor(i => i.SurvivorId).NotEmpty();
        RuleFor(i => i.LoserId).NotEmpty();

        RuleFor(i => i.LoserId)
            .NotEqual(i => i.SurvivorId)
            .WithMessage("A vessel cannot be merged into itself.");

        RuleFor(i => i.Reason).MaximumLength(500);
    }
}
