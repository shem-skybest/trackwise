using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeGensetsValidator : AbstractValidator<MergeGensetsCommand>
{
    public MergeGensetsValidator()
    {
        RuleFor(i => i.SurvivorId).NotEmpty();
        RuleFor(i => i.LoserId).NotEmpty();

        RuleFor(i => i.LoserId)
            .NotEqual(i => i.SurvivorId)
            .WithMessage("A genset cannot be merged into itself.");

        RuleFor(i => i.Reason).MaximumLength(500);
    }
}
